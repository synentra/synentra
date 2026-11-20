namespace Synentra.Domain.Execution;

public static class ExecutionTransitionPolicy
{
    public static bool CanTransition(ExecutionStatus from, ExecutionStatus to) => (from, to) switch
    {
        (ExecutionStatus.NotStarted, ExecutionStatus.Claimed or ExecutionStatus.Dispatching or ExecutionStatus.FailedBeforeDispatch or ExecutionStatus.CancelledBeforeDispatch) => true,
        (ExecutionStatus.Claimed, ExecutionStatus.Dispatching or ExecutionStatus.FailedBeforeDispatch or ExecutionStatus.CancelledBeforeDispatch) => true,
        (ExecutionStatus.Dispatching, ExecutionStatus.Responded or ExecutionStatus.Unknown) => true,
        (ExecutionStatus.Accepted, ExecutionStatus.Running or ExecutionStatus.Failed) => true,
        (ExecutionStatus.Running, ExecutionStatus.Completed or ExecutionStatus.Failed or ExecutionStatus.CancellationRequested) => true,
        (ExecutionStatus.CancellationRequested, ExecutionStatus.Cancelled or ExecutionStatus.Failed) => true,
        _ => false
    };

    public static void Ensure(ExecutionStatus from, ExecutionStatus to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException($"Invalid execution transition: {from} -> {to}.");
    }

    public static void Ensure(ExecutionStatus from, ExecutionStatus to, string message)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException(message);
    }
}