namespace Synentra.Domain.Governance.Trust.Events;

public sealed record TrustScoreChanged(
    string AgentId, 
    double OldScore, 
    double NewScore, 
    string Reason, 
    DateTimeOffset OccurredAt);
