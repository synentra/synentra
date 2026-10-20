namespace Synentra.Application.Features.Auditing.Contracts;

public sealed record AuditEntry(
    string Id, 
    string Category, 
    string ActorId, 
    string SecurityScope, 
    string? OperationId, 
    string? BindingId, 
    string? TargetId, 
    string? InvocationId, 
    string? InputDigest, 
    string? PolicyRevision, 
    string? ApprovalId, 
    IReadOnlyDictionary<string, string> Details, 
    DateTimeOffset Timestamp);
