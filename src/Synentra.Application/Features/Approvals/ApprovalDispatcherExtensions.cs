using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Approvals.Commands.ApproveInvocation;
using Synentra.Application.Features.Approvals.Commands.RejectInvocation;
using Synentra.Application.Features.Approvals.Commands.RevokeApproval;
using Synentra.Application.Features.Approvals.Queries.GetApproval;
using Synentra.Application.Features.Approvals.Queries.ListPendingApprovals;
using Synentra.Domain.Approvals;
using Synentra.Domain.Common.Results;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Features.Approvals;

public static class ApprovalDispatcherExtensions
{
    public static async Task<Approval?> GetApproval(
        this IDispatcher dispatcher,
        string approvalId,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new GetApprovalQuery(approvalId), cancellationToken);
    }

    public static async Task<PaginatedResult<Approval>> ListPendingApprovals(
        this IDispatcher dispatcher,
        string SecurityScope,
        int? pageNumber, 
        int? pageSize,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new ListPendingApprovalsQuery(SecurityScope, new PageRequest(pageNumber ?? 1, pageSize ?? 20)), cancellationToken);
    }

    public static async Task Approve(
        this IDispatcher dispatcher,
        string approvalId,
        CanonicalPrincipal reviewer,
        bool allowsSelfApproval,
        CancellationToken cancellationToken)
    {
        await dispatcher.Dispatch(new ApproveInvocationCommand(approvalId, reviewer, allowsSelfApproval), cancellationToken);
    }

    public static async Task Reject(
        this IDispatcher dispatcher,
        string approvalId,
        CanonicalPrincipal reviewer,
        string? reason,
        CancellationToken cancellationToken)
    {
        await dispatcher.Dispatch(new RejectInvocationCommand(approvalId, reviewer, reason), cancellationToken);
    }

    public static async Task Revoke(
        this IDispatcher dispatcher,
        string approvalId,
        CanonicalPrincipal reviewer,
        CancellationToken cancellationToken)
    {
        await dispatcher.Dispatch(new RevokeApprovalCommand(approvalId, reviewer), cancellationToken);
    }
}
