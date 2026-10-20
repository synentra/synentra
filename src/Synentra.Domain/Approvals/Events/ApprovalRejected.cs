namespace Synentra.Domain.Approvals.Events;

public sealed record ApprovalRejected(
    string ApprovalId, 
    string ReviewerId, 
    string? Reason, 
    DateTimeOffset OccurredAt);