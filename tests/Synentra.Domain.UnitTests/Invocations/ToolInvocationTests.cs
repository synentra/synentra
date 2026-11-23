using FluentAssertions;
using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Invocations;
using Synentra.Domain.Invocations.Events;
using Synentra.Domain.Invocations.ValueObjects;
using Synentra.Domain.Operations.ValueObjects;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class ToolInvocationTests
{
    [Fact]
    public void Constructor_ShouldCopyAndExposeInvocationData()
    {
        var id = InvocationId.New();
        var operationId = new OperationId("inventory.product.get");
        var resource = new ResourceDescriptor("product", "p-1", "tenant-a", "inventory");
        var resources = new List<ResourceDescriptor> { resource };
        var arguments = new Dictionary<string, object> { ["productId"] = "p-1" };
        var context = new Dictionary<string, string> { ["workflowId"] = "wf-1" };
        var transport = new Dictionary<string, string> { ["remoteIp"] = "127.0.0.1" };
        var digest = new BindingDigest(new string('A', 64));
        var receipt = DateTimeOffset.UtcNow;

        var invocation = new ToolInvocation(id, 2, "agent-1", "tenant-a", operationId, "1.0", "inventory",
            "get", "binding-1", "binding-v1", "inventory-api", resources, arguments, context, transport,
            digest, IntentModality.Http | IntentModality.FunctionCall, "Check inventory", "grant-1", "idem-1",
            true, false, receipt);

        invocation.Id.Should().Be(id);
        invocation.SchemaVersion.Should().Be(2);
        invocation.AuthenticatedAgentId.Should().Be("agent-1");
        invocation.SecurityScope.Should().Be("tenant-a");
        invocation.OperationId.Should().Be(operationId);
        invocation.OperationVersion.Should().Be("1.0");
        invocation.OperationFamily.Should().Be("inventory");
        invocation.Action.Should().Be("get");
        invocation.BindingId.Should().Be("binding-1");
        invocation.BindingVersion.Should().Be("binding-v1");
        invocation.TargetId.Should().Be("inventory-api");
        invocation.Resources.Should().ContainSingle().Which.Should().Be(resource);
        invocation.Arguments["productId"].Should().Be("p-1");
        invocation.Context["workflowId"].Should().Be("wf-1");
        invocation.TransportDetails["remoteIp"].Should().Be("127.0.0.1");
        invocation.Digest.Should().Be(digest);
        invocation.Modality.Should().Be(IntentModality.Http | IntentModality.FunctionCall);
        invocation.DeclaredPurpose.Should().Be("Check inventory");
        invocation.TaskGrantId.Should().Be("grant-1");
        invocation.IdempotencyKey.Should().Be("idem-1");
        invocation.RequiresGrant.Should().BeTrue();
        invocation.IsMutation.Should().BeFalse();
        invocation.ReceiptTime.Should().Be(receipt);
        invocation.HasEffectRegistration.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldRejectMissingAuthenticatedAgentId()
    {
        FluentActions.Invoking(() => new ToolInvocation(InvocationId.New(), 1, " ", "scope",
            new OperationId("op"), "v1", "family", "action", "binding", "v1", "target", [],
            new Dictionary<string, object>(), new Dictionary<string, string>(), new Dictionary<string, string>(),
            new BindingDigest(new string('a', 64)), IntentModality.None, null, null, null, false, false,
            DateTimeOffset.UtcNow))
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(InvocationDiagnostics.InvalidAuthenticatedAgentId);
    }

    [Fact]
    public void Constructor_ShouldFreezeDictionariesAsDefensiveCopies()
    {
        var arguments = new Dictionary<string, object> { ["initial"] = 1 };
        var context = new Dictionary<string, string> { ["initial"] = "before" };
        var transport = new Dictionary<string, string> { ["initial"] = "before" };
        var invocation = new ToolInvocation(InvocationId.New(), 1, "agent", "scope", new OperationId("op"),
            "v1", "family", "action", "binding", "v1", "target", [], arguments, context, transport,
            new BindingDigest(new string('a', 64)), IntentModality.Mcp, null, null, null, false, true,
            DateTimeOffset.UtcNow);

        arguments["initial"] = 2;
        context["initial"] = "after";
        transport["initial"] = "after";
        invocation.Arguments["initial"].Should().Be(1);
        invocation.Context["initial"].Should().Be("before");
        invocation.TransportDetails["initial"].Should().Be("before");

        FluentActions.Invoking(() => ((IDictionary<string, object>)invocation.Arguments)["new"] = 3)
            .Should().Throw<NotSupportedException>();
        FluentActions.Invoking(() => ((IDictionary<string, string>)invocation.Context)["new"] = "value")
            .Should().Throw<NotSupportedException>();
        FluentActions.Invoking(() => ((IDictionary<string, string>)invocation.TransportDetails)["new"] = "value")
            .Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void InvocationDecisionMade_ShouldExposeConstructorData()
    {
        var id = InvocationId.New();
        var now = DateTimeOffset.UtcNow;
        var decision = new InvocationDecisionMade(id, Synentra.Domain.Governance.Decisions.DecisionOutcome.Review,
            ["REVIEW_REQUIRED"], "policy-v4", now);

        decision.InvocationId.Should().Be(id);
        decision.Outcome.Should().Be(Synentra.Domain.Governance.Decisions.DecisionOutcome.Review);
        decision.ReasonCodes.Should().Equal("REVIEW_REQUIRED");
        decision.PolicyRevision.Should().Be("policy-v4");
        decision.OccurredAt.Should().Be(now);
    }
}
