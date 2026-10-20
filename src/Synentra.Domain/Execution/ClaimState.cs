namespace Synentra.Domain.Execution;

public enum ClaimState
{
    Claimed,
    Dispatching,
    Responded,
    FailedBeforeDispatch,
    Unknown,
    CancelledBeforeDispatch,
    Expired
}
