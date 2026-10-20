namespace Synentra.Domain.Security.ValueObjects;

public sealed record ExecutionIdentity(
    string AgentId, 
    string SecurityScope, 
    string AuthenticatedSubject, 
    string? DelegatedUserId,
    string? Issuer);
