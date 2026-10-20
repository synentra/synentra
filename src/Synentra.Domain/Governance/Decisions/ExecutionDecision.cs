using Synentra.Domain.Governance.Evidence;

namespace Synentra.Domain.Governance.Decisions;

public sealed class ExecutionDecision
{
    public DecisionOutcome Outcome { get; }
    public IReadOnlyList<string> ReasonCodes { get; }
    public GovernanceEvidence Evidence { get; }
    public string PolicyRevision { get; }
    public DateTimeOffset ExpiresAt { get; }
    public string? ObligationId { get; }

    private ExecutionDecision(
        DecisionOutcome outcome,
        IReadOnlyList<string> reasonCodes,
        GovernanceEvidence evidence,
        string policyRevision,
        DateTimeOffset expiresAt,
        string? obligationId)
    {
        Outcome = outcome;
        ReasonCodes = reasonCodes;
        Evidence = evidence;
        PolicyRevision = policyRevision;
        ExpiresAt = expiresAt;
        ObligationId = obligationId;
    }

    public static ExecutionDecision Allow(
        IReadOnlyList<string> reasons, 
        GovernanceEvidence? evidence = null, 
        string policyRevision = "unknown", 
        TimeSpan? ttl = null) 
        => new(DecisionOutcome.Allow, reasons, evidence 
            ?? new GovernanceEvidence(), policyRevision, DateTimeOffset.UtcNow + (ttl ?? TimeSpan.FromMinutes(15)), null);

    public static ExecutionDecision Review(
        IReadOnlyList<string> reasons, 
        GovernanceEvidence? evidence = null, 
        string policyRevision = "unknown", 
        TimeSpan? ttl = null) 
        => new(DecisionOutcome.Review, reasons, evidence 
            ?? new GovernanceEvidence(), policyRevision, DateTimeOffset.UtcNow + (ttl ?? TimeSpan.FromMinutes(15)), null);

    public static ExecutionDecision Deny(
        IReadOnlyList<string> reasons, 
        GovernanceEvidence? evidence = null, 
        string policyRevision = "unknown") 
        => new(DecisionOutcome.Deny, reasons, evidence 
            ?? new GovernanceEvidence(), policyRevision, DateTimeOffset.UtcNow, null);
}
