namespace Synentra.Application.Features.Auditing.Contracts;

public sealed record AuditQuery(
    string? SecurityScope, 
    string? ActorId, 
    string? InvocationId, 
    string? Category, 
    DateTimeOffset? From, 
    DateTimeOffset? To);
