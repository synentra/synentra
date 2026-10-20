using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;
using Synentra.Domain.Invocations.ValueObjects;

namespace Synentra.Domain.Approvals;

public sealed class Approval
{
    public string Id { get; }
    public InvocationId InvocationId { get; }
    public BindingDigest BindingDigest { get; }
    public string RequestingAgentId { get; }
    public string OperationId { get; }
    public string BindingId { get; }
    public string? TaskGrantId { get; }
    public string PolicyRevision { get; }
    public string ReviewObligationRevision { get; }
    public ApprovalStatus Status { get; private set; }
    public string? ReviewerId { get; private set; }
    public string SecurityScope { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public Approval(string id, InvocationId invocationId, 
        BindingDigest bindingDigest, string requestingAgentId, 
        string operationId, string bindingId, 
        string? taskGrantId, string policyRevision, 
        string reviewObligationRevision, string securityScope, 
        DateTimeOffset expiresAt)
    {
        Id = id;
        InvocationId = invocationId;
        BindingDigest = bindingDigest;
        RequestingAgentId = requestingAgentId;
        OperationId = operationId;
        BindingId = bindingId;
        TaskGrantId = taskGrantId;
        PolicyRevision = policyRevision;
        ReviewObligationRevision = reviewObligationRevision;
        SecurityScope = securityScope;
        Status = ApprovalStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    public void Approve(string reviewerId)
    {
        if (Status != ApprovalStatus.Pending)
            throw new DomainException(DomainErrors.Approval.CannotApprove(Status.ToString()));

        if (DateTimeOffset.UtcNow >= ExpiresAt)
            throw new DomainException(DomainErrors.Approval.ApprovalHasExpired);

        Status = ApprovalStatus.Approved;
        ReviewerId = reviewerId;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string reviewerId)
    {
        if (Status != ApprovalStatus.Pending)
            throw new DomainException(DomainErrors.Approval.CannotReject(Status.ToString()));

        Status = ApprovalStatus.Rejected;
        ReviewerId = reviewerId;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke()
    {
        if (Status is ApprovalStatus.Rejected or ApprovalStatus.Expired)
            throw new DomainException(DomainErrors.Approval.CannotRevoke(Status.ToString()));

        Status = ApprovalStatus.Revoked;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    public void Consume()
    {
        if (Status != ApprovalStatus.Approved)
            throw new DomainException(DomainErrors.Approval.CannotConsume(Status.ToString()));

        Status = ApprovalStatus.Consumed;
    }

    public static Approval Restore(
        string id, InvocationId invocationId, 
        BindingDigest digest, string agentId, 
        string operationId, string bindingId, 
        string? grantId, string policyRevision, 
        string reviewRevision, string scope, 
        DateTimeOffset expiresAt, DateTimeOffset createdAt, 
        ApprovalStatus status, string? reviewerId, 
        DateTimeOffset? resolvedAt) 
        => new(id, invocationId, digest, agentId, 
            operationId, bindingId, 
            grantId, policyRevision, 
            reviewRevision, scope, 
            expiresAt)
    {
        CreatedAt = createdAt,
        Status = status,
        ReviewerId = reviewerId,
        ResolvedAt = resolvedAt
    };
}
