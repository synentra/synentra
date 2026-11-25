using FluentAssertions;
using NSubstitute;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.TaskGrants;
using Synentra.Application.Features.TaskGrants.Commands.IssueTaskGrant;
using Synentra.Application.Features.TaskGrants.Commands.RevokeTaskGrant;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Application.Features.TaskGrants.Queries.GetTaskGrant;
using Synentra.Application.Features.TaskGrants.Queries.ListActiveTaskGrants;
using Synentra.Application.Tests.TestHelpers;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Grants;
using Synentra.Domain.Security;
using Void = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Tests.Features.TaskGrants;

public sealed class TaskGrantTests
{
    private readonly IGrantStore _store = Substitute.For<IGrantStore>();

    [Fact]
    public async Task IssueTaskGrantCommandHandler_ShouldEnforceIssuerAgentIdAndPersist()
    {
        var handler = new IssueTaskGrantCommandHandler(_store);
        var issuer = TestDataBuilder.CreatePrincipal("actual-issuer", "scope-1", PrincipalType.TrustedIssuer);
        var requested = TestDataBuilder.CreateTaskGrant(id: "g-1", issuerId: "spoofed-issuer", agentId: "agent-1");

        var effective = await handler.Handle(new IssueTaskGrantCommand(issuer, requested), TestContext.Current.CancellationToken);

        effective.Id.Should().Be("g-1");
        effective.IssuerId.Should().Be("actual-issuer");
        effective.AgentId.Should().Be("agent-1");
        await _store.Received(1).SaveAsync(effective, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeTaskGrantCommandHandler_WhenIssuerOrAdmin_ShouldRevokeAndSave()
    {
        var grant = TestDataBuilder.CreateTaskGrant(id: "g-1", issuerId: "issuer-1");
        _store.GetAsync("g-1", Arg.Any<CancellationToken>()).Returns(grant);

        var handler = new RevokeTaskGrantCommandHandler(_store);
        var issuer = TestDataBuilder.CreatePrincipal("issuer-1", type: PrincipalType.TrustedIssuer);

        var res = await handler.Handle(new RevokeTaskGrantCommand("g-1", issuer), TestContext.Current.CancellationToken);

        res.Should().Be(Void.Value);
        grant.IsValidAt(DateTimeOffset.UtcNow).Should().BeFalse();
        await _store.Received(1).SaveAsync(grant, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeTaskGrantCommandHandler_WhenNotFoundOrUnauthorized_ShouldThrowSynentraApplicationException()
    {
        _store.GetAsync("missing", Arg.Any<CancellationToken>()).Returns((TaskGrant?)null);
        var handler = new RevokeTaskGrantCommandHandler(_store);
        var admin = TestDataBuilder.CreatePrincipal("admin-1", type: PrincipalType.Admin);

        await FluentActions.Invoking(() => handler.Handle(new RevokeTaskGrantCommand("missing", admin)))
            .Should().ThrowAsync<SynentraApplicationException>();

        var grant = TestDataBuilder.CreateTaskGrant(id: "g-1", issuerId: "issuer-1");
        _store.GetAsync("g-1", Arg.Any<CancellationToken>()).Returns(grant);
        var unauthorized = TestDataBuilder.CreatePrincipal("other-agent", type: PrincipalType.Agent);

        await FluentActions.Invoking(() => handler.Handle(new RevokeTaskGrantCommand("g-1", unauthorized)))
            .Should().ThrowAsync<SynentraApplicationException>();
    }

    [Fact]
    public async Task QueriesServiceExtensionsAndValidators_ShouldWorkAsExpected()
    {
        var grant = TestDataBuilder.CreateTaskGrant("g-1");
        _store.GetAsync("g-1", Arg.Any<CancellationToken>()).Returns(grant);
        _store.FindActiveForAgentAsync("agent-1", "scope-1", Arg.Any<CancellationToken>()).Returns(new[] { grant });

        var getHandler = new GetTaskGrantQueryHandler(_store);
        (await getHandler.Handle(new GetTaskGrantQuery("g-1"), TestContext.Current.CancellationToken)).Should().BeSameAs(grant);

        var listHandler = new ListActiveTaskGrantsQueryHandler(_store);
        (await listHandler.Handle(new ListActiveTaskGrantsQuery("agent-1", "scope-1"), TestContext.Current.CancellationToken)).Should().ContainSingle().Which.Should().BeSameAs(grant);

        var principal = TestDataBuilder.CreatePrincipal("issuer-1", type: PrincipalType.TrustedIssuer);
        new IssueTaskGrantCommandValidator().Validate(new IssueTaskGrantCommand(null!, grant)).IsValid.Should().BeFalse();
        new IssueTaskGrantCommandValidator().Validate(new IssueTaskGrantCommand(principal, null!)).IsValid.Should().BeFalse();
        new IssueTaskGrantCommandValidator().Validate(new IssueTaskGrantCommand(principal, grant)).IsValid.Should().BeTrue();

        new RevokeTaskGrantCommandValidator().Validate(new RevokeTaskGrantCommand("", principal)).IsValid.Should().BeFalse();
        new RevokeTaskGrantCommandValidator().Validate(new RevokeTaskGrantCommand("g-1", null!)).IsValid.Should().BeFalse();
        new RevokeTaskGrantCommandValidator().Validate(new RevokeTaskGrantCommand("g-1", principal)).IsValid.Should().BeTrue();

        new GetTaskGrantQueryValidator().Validate(new GetTaskGrantQuery("")).IsValid.Should().BeFalse();
        new GetTaskGrantQueryValidator().Validate(new GetTaskGrantQuery("g-1")).IsValid.Should().BeTrue();

        new ListActiveTaskGrantsQueryValidator().Validate(new ListActiveTaskGrantsQuery("", "scope")).IsValid.Should().BeFalse();
        new ListActiveTaskGrantsQueryValidator().Validate(new ListActiveTaskGrantsQuery("agent", "")).IsValid.Should().BeFalse();
        new ListActiveTaskGrantsQueryValidator().Validate(new ListActiveTaskGrantsQuery("agent", "scope")).IsValid.Should().BeTrue();

        var dispatcher = Substitute.For<IDispatcher>();
        var service = new TaskGrantService(dispatcher);
        await service.IssueAsync(principal, grant, TestContext.Current.CancellationToken);
        await service.RevokeAsync("g-1", principal, TestContext.Current.CancellationToken);

        await dispatcher.GetTaskGrant("g-1", TestContext.Current.CancellationToken);
        await dispatcher.IssueTaskGrant(principal, grant, TestContext.Current.CancellationToken);
        await dispatcher.RevokeTaskGrant("g-1", principal, TestContext.Current.CancellationToken);

        await dispatcher.Received(2).Dispatch(Arg.Any<IssueTaskGrantCommand>(), Arg.Any<CancellationToken>());
        await dispatcher.Received(2).Dispatch(Arg.Any<RevokeTaskGrantCommand>(), Arg.Any<CancellationToken>());
        await dispatcher.Received(1).Dispatch(Arg.Any<GetTaskGrantQuery>(), Arg.Any<CancellationToken>());
    }
}
