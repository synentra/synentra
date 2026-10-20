using Synentra.Domain.Security;
using Synentra.Application.Common.Diagnostics;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Application.Features.Auditing.Contracts;
using Unit = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Features.Approvals.Commands.ApproveInvocation;

public sealed class ApproveInvocationCommandHandler(
    IApprovalStore store, 
    IAuditStore audit, 
    IAdminPolicy adminPolicy) : IActionHandler<ApproveInvocationCommand, Unit>
{
    public async Task<Unit> Handle(ApproveInvocationCommand request, CancellationToken cancellationToken = default)
    {
        var approval = await store.GetAsync(request.ApprovalId, cancellationToken) 
            ?? throw new SynentraApplicationException(ApplicationError.Approval.ApprovalNotFound(request.ApprovalId));

        var reviewer = request.Reviewer;
        if (reviewer.Type is not (PrincipalType.Reviewer or PrincipalType.Admin))
            throw new SynentraApplicationException(ApplicationError.Approval.ApprovalReviewerPermissionRequired);

        if (!adminPolicy.AllowsCrossScopeApproval && reviewer.SecurityScope != approval.SecurityScope)
            throw new SynentraApplicationException(ApplicationError.Approval.ApprovalReviewerPermissionRequired);

        if (approval.RequestingAgentId == reviewer.AgentId && !(request.AllowsSelfApproval && adminPolicy.AllowsSelfApproval))
            throw new SynentraApplicationException(ApplicationError.Approval.ApprovalSelfApprovalDenied);

        approval.Approve(reviewer.AgentId);
        await store.SaveAsync(approval, cancellationToken);

        await audit.AppendAsync(new AuditEntry(
                Guid.NewGuid().ToString("N"), 
                "approval-approved", 
                reviewer.AgentId, 
                reviewer.SecurityScope, 
                approval.OperationId, 
                approval.BindingId, 
                null, 
                approval.InvocationId.ToString(), 
                approval.BindingDigest.Value, 
                approval.PolicyRevision, 
                approval.Id, 
                new Dictionary<string, string> { ["reviewer"] = reviewer.AgentId }, 
                DateTimeOffset.UtcNow), 
            cancellationToken);

        return Unit.Value;
    }
}
