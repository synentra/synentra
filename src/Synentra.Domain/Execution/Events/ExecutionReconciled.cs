namespace Synentra.Domain.Execution.Events;

public sealed record ExecutionReconciled(
    string ExecutionId, 
    string ReconciledBy, 
    string PredicateVersion, 
    DateTimeOffset OccurredAt);
