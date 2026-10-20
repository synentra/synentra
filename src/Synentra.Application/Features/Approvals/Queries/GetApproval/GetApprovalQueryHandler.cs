using Synentra.Domain.Approvals;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Approvals.Contracts;

namespace Synentra.Application.Features.Approvals.Queries.GetApproval;

public sealed class GetApprovalQueryHandler : IActionHandler<GetApprovalQuery, Approval?>
{
    private readonly IApprovalStore _store;

    public GetApprovalQueryHandler(IApprovalStore store)
    {
        _store = store;
    }

    public async Task<Approval?> Handle(GetApprovalQuery request, CancellationToken cancellationToken = default)
    {
        var approvalId = request.ApprovalId;
        var ct = cancellationToken;
        return await _store.GetAsync(approvalId, ct);
    }
}
