using FluentAssertions;
using NSubstitute;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Plugins;
using Synentra.Application.Features.Auditing;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Auditing.Queries.QueryAudit;
using Synentra.Application.Features.Capabilities;
using Synentra.Application.Features.Capabilities.Queries.GetCapabilities;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Models;
using Synentra.Application.Features.Operations;
using Synentra.Application.Features.Operations.Queries.ListOperations;
using Synentra.Application.Tests.TestHelpers;
using Synentra.Domain.Common.Results;

namespace Synentra.Application.Tests.Features.AuditingCapabilitiesOperations;

public sealed class FeatureQueryAndExtensionTests
{
    [Fact]
    public async Task QueryAuditQueryHandler_ShouldReturnPagedAuditEntries()
    {
        var store = Substitute.For<IAuditStore>();
        var filter = new AuditQuery("scope-1", null, null, "decision", null, null);
        var pageRequest = new PageRequest(1, 25);
        var entry = new AuditEntry("a1", "decision", "agent-1", "scope-1", "op-1", "b-1", "t-1", "inv-1", "dig", "rev-1", null, new Dictionary<string, string>(), DateTimeOffset.UtcNow);
        store.QueryAsync(filter, pageRequest, Arg.Any<CancellationToken>())
            .Returns(new Page<AuditEntry>(new[] { entry }, 1));

        var handler = new QueryAuditQueryHandler(store);
        var result = await handler.Handle(new QueryAuditQuery(filter, pageRequest), TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle().Which.Should().Be(entry);
        result.TotalCount.Should().Be(1);

        new QueryAuditQueryValidator().Validate(new QueryAuditQuery(null!, pageRequest)).IsValid.Should().BeFalse();
        new QueryAuditQueryValidator().Validate(new QueryAuditQuery(filter, pageRequest)).IsValid.Should().BeTrue();

        var dispatcher = Substitute.For<IDispatcher>();
        await dispatcher.QueryAudits(filter, 1, 25, TestContext.Current.CancellationToken);
        await dispatcher.Received(1).Dispatch(Arg.Any<QueryAuditQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCapabilitiesQueryHandler_ShouldReturnOnlyTransportAdaptersSortedById()
    {
        var catalog = new PluginCatalog();
        catalog.Register(TestDataBuilder.CreatePluginRegistration("z-adapter", "1.0", PluginFamily.TransportAdapter, "v2", "requestResponse", "customCap", "asynchronous"));
        catalog.Register(TestDataBuilder.CreatePluginRegistration("a-adapter", "2.0", PluginFamily.TransportAdapter, "v2", "batch", "stream"));
        catalog.Register(TestDataBuilder.CreatePluginRegistration("risk-plugin", "1.0", PluginFamily.RiskCalculator, "v2", "score"));

        var handler = new GetCapabilitiesQueryHandler(catalog);
        var response = await handler.Handle(new GetCapabilitiesQuery(), TestContext.Current.CancellationToken);

        response.Adapters.Should().HaveCount(2);
        response.Adapters[0].AdapterId.Should().Be("a-adapter");
        response.Adapters[0].SupportedExecutionModels.Should().Equal("batch", "stream");
        response.Adapters[1].AdapterId.Should().Be("z-adapter");
        response.Adapters[1].SupportedExecutionModels.Should().Equal("asynchronous", "requestResponse");
        response.Adapters[1].EnforcementCapabilities.Should().Equal("asynchronous", "customCap", "requestResponse");

        var dispatcher = Substitute.For<IDispatcher>();
        await dispatcher.GetCapabilities(TestContext.Current.CancellationToken);
        await dispatcher.Received(1).Dispatch(Arg.Any<GetCapabilitiesQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListOperationsQueryHandler_WhenNoActiveBundle_ShouldReturnEmpty()
    {
        var manifests = Substitute.For<IManifestProvider>();
        var revisions = Substitute.For<IRevisionProvider>();
        manifests.ActiveBundle.Returns((GovernanceBundleModel?)null);

        var handler = new ListOperationsQueryHandler(manifests, revisions);
        var result = await handler.Handle(new ListOperationsQuery("agent-1", new PageRequest(1, 10)), TestContext.Current.CancellationToken);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        await revisions.Received(1).ReloadAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListOperationsQueryHandler_ShouldExcludeHardDeniedOperationsAndPaginate()
    {
        var manifests = Substitute.For<IManifestProvider>();
        var revisions = Substitute.For<IRevisionProvider>();
        var bundle = TestDataBuilder.CreateValidBundle();
        bundle.Spec.Operations.Add(new OperationModel
        {
            Id = "admin.nuke",
            Version = "1",
            Family = "admin",
            Action = "nuke",
            Mutability = "mutation",
            Execution = new ExecutionMode { Mode = "requestResponse" }
        });
        bundle.Spec.Restrictions.Add(new RestrictionModel
        {
            Id = "deny-admin-for-agent-1",
            Effect = "deny",
            Match = new RestrictionMatchModel
            {
                AgentIds = ["agent-1"],
                OperationFamilies = ["admin"]
            }
        });
        manifests.ActiveBundle.Returns(bundle);

        var handler = new ListOperationsQueryHandler(manifests, revisions);
        var result = await handler.Handle(new ListOperationsQuery("agent-1", new PageRequest(1, 10)), TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be("orders.create");
        result.Items[0].BindingIds.Should().Equal("orders-http");

        var dispatcher = Substitute.For<IDispatcher>();
        await dispatcher.ListOperations("agent-1", null, null, TestContext.Current.CancellationToken);
        await dispatcher.Received(1).Dispatch(Arg.Is<ListOperationsQuery>(q => q.PageRequest.PageNumber == 1 && q.PageRequest.PageSize == 20), Arg.Any<CancellationToken>());
    }
}
