namespace Synentra.Domain.Execution;

public enum ExecutionStatus
{
    NotStarted,
    Claimed,
    Dispatching,
    Responded,
    FailedBeforeDispatch,
    Unknown,
    CancelledBeforeDispatch,
    // async extensions
    Accepted,
    Running,
    Completed,
    Failed,
    CancellationRequested,
    Cancelled
}
