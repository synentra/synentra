namespace Synentra.Domain.Approvals.Events;

public sealed record ApprovalApproved(
    string ApprovalId, 
    string ReviewerId, 
    DateTimeOffset OccurredAt);