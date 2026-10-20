using Synentra.Domain.Execution;

namespace Synentra.Application.Features.Invocations.Contracts;

public static class LifecycleStatusNames
{
    public static string Execution(ExecutionStatus status) => status switch
    {
        ExecutionStatus.NotStarted => "not_started",
        ExecutionStatus.Claimed => "claimed",
        ExecutionStatus.Dispatching => "dispatching",
        ExecutionStatus.Responded => "responded",
        ExecutionStatus.FailedBeforeDispatch => "failed_before_dispatch",
        ExecutionStatus.Unknown => "unknown",
        ExecutionStatus.CancelledBeforeDispatch => "cancelled_before_dispatch",
        ExecutionStatus.Accepted => "accepted",
        ExecutionStatus.Running => "running",
        ExecutionStatus.Completed => "completed",
        ExecutionStatus.Failed => "failed",
        ExecutionStatus.CancellationRequested => "cancellation_requested",
        ExecutionStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static string Outcome(OutcomeStatus status) => status switch
    {
        OutcomeStatus.NotChecked => "not_checked",
        OutcomeStatus.Pending => "pending",
        OutcomeStatus.Verified => "verified",
        OutcomeStatus.Contradicted => "contradicted",
        OutcomeStatus.Unknown => "unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
