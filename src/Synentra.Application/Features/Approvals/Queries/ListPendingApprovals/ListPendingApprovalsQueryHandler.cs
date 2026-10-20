using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Domain.Approvals;
using Synentra.Domain.Common.Results;

namespace Synentra.Application.Features.Approvals.Queries.ListPendingApprovals;

public sealed class ListPendingApprovalsQueryHandler : IActionHandler<ListPendingApprovalsQuery, PaginatedResult<Approval>>
{
    private readonly IApprovalStore _approvalRepository;

    public ListPendingApprovalsQueryHandler(IApprovalStore approvalRepository)
    {
        _approvalRepository = approvalRepository;
    }

    public async Task<PaginatedResult<Approval>> Handle(ListPendingApprovalsQuery request, CancellationToken cancellationToken = default)
    {
        var page = await _approvalRepository.ListPendingAsync(
            request.SecurityScope,
            request.PageRequest, 
            cancellationToken);

        return await PaginatedResult<Approval>.SuccessAsync(page.Items, request.PageRequest, page.TotalCount);
    }
}
