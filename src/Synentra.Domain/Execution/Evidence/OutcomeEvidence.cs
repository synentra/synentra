namespace Synentra.Domain.Execution.Evidence;

public sealed class OutcomeEvidence
{
    public string Id { get; }
    public string ExecutionId { get; }
    public OutcomeStatus Status { get; }
    public string? Source { get; }
    public string? PredicateVersion { get; }
    public string? SafeSummary { get; }
    public string? EvidenceDigest { get; }
    public DateTimeOffset ObservedAt { get; private set; }
    public string? ContradictionReason { get; }

    public static OutcomeEvidence Restore(
        string id, string executionId, 
        OutcomeStatus status, string? source, 
        string? predicateVersion, string? safeSummary, 
        string? evidenceDigest, string? contradictionReason, 
        DateTimeOffset observedAt) 
        => new(id, executionId, status, source, predicateVersion, safeSummary, evidenceDigest, contradictionReason)
        {
            ObservedAt = observedAt
        };

    public OutcomeEvidence(
        string id, string executionId, 
        OutcomeStatus status, string? source, 
        string? predicateVersion, string? safeSummary, 
        string? evidenceDigest, string? contradictionReason = null)
    {
        Id = id;
        ExecutionId = executionId;
        Status = status;
        Source = source;
        PredicateVersion = predicateVersion;
        SafeSummary = safeSummary;
        EvidenceDigest = evidenceDigest;
        ObservedAt = DateTimeOffset.UtcNow;
        ContradictionReason = contradictionReason;
    }
}
