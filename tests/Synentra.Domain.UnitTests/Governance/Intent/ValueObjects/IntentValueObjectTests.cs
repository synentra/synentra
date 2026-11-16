using FluentAssertions;
using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Domain.Invocations;
using Synentra.Domain.Invocations.ValueObjects;
using Synentra.Domain.Operations.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class IntentValueObjectTests
{
    [Theory]
    [InlineData(0d)]
    [InlineData(0.5d)]
    [InlineData(1d)]
    public void IntentLabel_ShouldAcceptInclusiveConfidenceRange(double confidence)
    {
        var label = new IntentLabel("safe_read", confidence, IntentStatus.Available, "model-v1");
        label.Value.Should().Be("safe_read");
        label.Confidence.Should().Be(confidence);
        label.Status.Should().Be(IntentStatus.Available);
        label.ProviderVersion.Should().Be("model-v1");
    }

    [Theory]
    [InlineData(-0.1d)]
    [InlineData(1.1d)]
    public void IntentLabel_ShouldRejectConfidenceOutsideRange(double confidence)
        => FluentActions.Invoking(() => new IntentLabel("x", confidence, IntentStatus.Error, "v1"))
            .Should().Throw<DomainException>()
            .Which.Diagnostic.Should().Be(DomainErrors.Intent.ConfidenceOutOfRange);

    [Fact]
    public void IntentEvidence_ShouldPreserveAllInputs()
    {
        var arguments = new Dictionary<string, object> { ["id"] = "123" };
        var context = new Dictionary<string, string> { ["workflow"] = "wf-1" };
        var evidence = new IntentEvidence("inventory.product.get", "inventory", "get", arguments,
            context, IntentModality.Http | IntentModality.Mcp, "Check stock", "agent-1", "tenant-a");

        evidence.OperationId.Should().Be("inventory.product.get");
        evidence.AuthenticatedAgentId.Should().Be("agent-1");
        evidence.SecurityScope.Should().Be("tenant-a");
        evidence.OperationFamily.Should().Be("inventory");
        evidence.Action.Should().Be("get");
        evidence.Arguments.Should().BeSameAs(arguments);
        evidence.Context.Should().BeSameAs(context);
        evidence.Modality.Should().Be(IntentModality.Http | IntentModality.Mcp);
        evidence.DeclaredPurpose.Should().Be("Check stock");
    }

    [Fact]
    public void IntentEvidence_FromInvocation_ShouldMapInvocationFields()
    {
        var invocation = CreateInvocation();

        var evidence = IntentEvidence.From(invocation);

        evidence.OperationId.Should().Be(invocation.OperationId.Value);
        evidence.AuthenticatedAgentId.Should().Be(invocation.AuthenticatedAgentId);
        evidence.SecurityScope.Should().Be(invocation.SecurityScope);
        evidence.OperationFamily.Should().Be(invocation.OperationFamily);
        evidence.Action.Should().Be(invocation.Action);
        evidence.Arguments.Should().BeSameAs(invocation.Arguments);
        evidence.Context.Should().BeSameAs(invocation.Context);
        evidence.Modality.Should().Be(invocation.Modality);
        evidence.DeclaredPurpose.Should().Be(invocation.DeclaredPurpose);
    }

    internal static ToolInvocation CreateInvocation()
        => new(InvocationId.New(), 2, "agent-1", "tenant-a", new OperationId("inventory.product.get"),
            "1.0", "inventory", "get", "binding-1", "binding-v1", "inventory-api", [],
            new Dictionary<string, object> { ["productId"] = "prod-1" },
            new Dictionary<string, string> { ["workflowId"] = "wf-1" },
            new Dictionary<string, string> { ["remoteIp"] = "127.0.0.1" },
            new BindingDigest(new string('a', 64)), IntentModality.Http, "Check stock", null, "idem-1",
            false, false, DateTimeOffset.UtcNow);
}
