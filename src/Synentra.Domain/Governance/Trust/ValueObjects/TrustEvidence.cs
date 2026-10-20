namespace Synentra.Domain.Governance.Trust.ValueObjects;

public sealed record TrustEvidence(
    string AgentId, 
    string SecurityScope, 
    TrustEvidenceType Type, 
    string EvidenceId, 
    string Description, 
    DateTimeOffset ObservedAt, 
    IReadOnlyDictionary<string, string>? Metadata = null);
