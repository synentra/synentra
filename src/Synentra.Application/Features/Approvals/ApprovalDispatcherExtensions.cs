using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Approvals.Queries.GetApproval;
using Synentra.Application.Features.Approvals.Queries.ListPendingApprovals;
using Synentra.Domain.Approvals;
using Synentra.Domain.Common.Results;

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
}
