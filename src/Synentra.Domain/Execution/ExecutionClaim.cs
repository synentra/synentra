using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;
using Synentra.Domain.Invocations.ValueObjects;

namespace Synentra.Domain.Execution;

public sealed class ExecutionClaim
{
    public InvocationId InvocationId { get; }
    public string ClaimedBy { get; }
    public DateTimeOffset ClaimedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public ClaimState State { get; private set; }

    public ExecutionClaim(
        InvocationId invocationId, 
        string claimedBy, 
        DateTimeOffset claimedAt, 
        DateTimeOffset expiresAt)
    {
        InvocationId = invocationId;
        ClaimedBy = claimedBy;
        ClaimedAt = claimedAt;
        ExpiresAt = expiresAt;
        State = ClaimState.Claimed;
    }

    public void TransitionToDispatching()
    {
        if (State != ClaimState.Claimed)
            throw new DomainException(DomainErrors.ExecutionClaim.CannotTransitionToDispatching(State.ToString()));
        State = ClaimState.Dispatching;
    }
}
