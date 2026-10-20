using Synentra.Domain.Invocations.ValueObjects;

namespace Synentra.Domain.Execution;

public sealed class ExecutionRecord
{
    public string Id { get; }
    public InvocationId InvocationId { get; }
    public ExecutionStatus Status { get; private set; }
    public string? UpstreamReference { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ResponsePayload { get; private set; }
    public int? ResponseStatusCode { get; private set; }

    public ExecutionRecord(string id, InvocationId invocationId)
    {
        Id = id;
        InvocationId = invocationId;
        Status = ExecutionStatus.NotStarted;
    }

    public void MarkClaimed()
    {
        if (Status != ExecutionStatus.NotStarted) 
            throw new InvalidOperationException("Execution is not awaiting a claim.");

        Status = ExecutionStatus.Claimed;
    }

    public void MarkDispatching()
    {
        if (Status is not (ExecutionStatus.NotStarted or ExecutionStatus.Claimed)) 
            throw new InvalidOperationException("Execution is not dispatchable.");

        Status = ExecutionStatus.Dispatching;
        DispatchedAt = DateTimeOffset.UtcNow;
    }

    public void MarkResponded(
        string? upstreamRef, string? responsePayload = null, 
        int? responseStatusCode = null, string? errorMessage = null)
    {
        Status = ExecutionStatus.Responded;
        UpstreamReference = upstreamRef;
        ResponsePayload = responsePayload;
        ResponseStatusCode = responseStatusCode;
        ErrorMessage = errorMessage;
        RespondedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailedBeforeDispatch(string reason)
    {
        Status = ExecutionStatus.FailedBeforeDispatch;
        ErrorMessage = reason;
        RespondedAt = DateTimeOffset.UtcNow;
    }

    public void MarkUnknown(string reason)
    {
        Status = ExecutionStatus.Unknown;
        ErrorMessage = reason;
    }

    public void MarkCancelledBeforeDispatch()
    {
        if (Status is not (ExecutionStatus.NotStarted or ExecutionStatus.Claimed)) 
            throw new InvalidOperationException("Execution already started.");

        Status = ExecutionStatus.CancelledBeforeDispatch;
        RespondedAt = DateTimeOffset.UtcNow;
    }

    public static ExecutionRecord Restore(
        string id,
        InvocationId invocationId,
        ExecutionStatus status,
        string? upstream,
        DateTimeOffset? dispatched,
        DateTimeOffset? responded,
        string? error,
        string? responsePayload = null,
        int? responseStatusCode = null) => new(id, invocationId)
        {
            Status = status,
            UpstreamReference = upstream,
            DispatchedAt = dispatched,
            RespondedAt = responded,
            ErrorMessage = error,
            ResponsePayload = responsePayload,
            ResponseStatusCode = responseStatusCode
        };
}
