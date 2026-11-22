using FluentAssertions;
using Synentra.Domain.Common.Exceptions;
using Synentra.Domain.Execution;
using Synentra.Domain.Invocations.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class ExecutionClaimTests
{
    private static readonly InvocationId Invocation = InvocationId.New();

    [Fact]
    public void Constructor_ShouldStartClaimed()
    {
        var claimedAt = DateTimeOffset.UtcNow;
        var expiresAt = claimedAt.AddMinutes(5);
        var claim = new ExecutionClaim(Invocation, "worker-1", claimedAt, expiresAt);

        claim.InvocationId.Should().Be(Invocation);
        claim.ClaimedBy.Should().Be("worker-1");
        claim.ClaimedAt.Should().Be(claimedAt);
        claim.ExpiresAt.Should().Be(expiresAt);
        claim.State.Should().Be(ClaimState.Claimed);
    }

    [Fact]
    public void TransitionToDispatching_ShouldTransitionClaimedState()
    {
        var claim = new ExecutionClaim(Invocation, "worker", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(1));

        claim.TransitionToDispatching();

        claim.State.Should().Be(ClaimState.Dispatching);
    }

    [Fact]
    public void TransitionToDispatching_ShouldRejectAnyOtherState()
    {
        var claim = new ExecutionClaim(Invocation, "worker", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(1));
        claim.TransitionToDispatching();

        FluentActions.Invoking(claim.TransitionToDispatching)
            .Should().Throw<DomainException>()
            .Which.Diagnostic.Code.Should().Be("SYNENTRA-EXE-0001");
    }
}
