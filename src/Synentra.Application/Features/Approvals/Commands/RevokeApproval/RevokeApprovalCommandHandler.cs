using Synentra.Domain.Security;
using Synentra.Application.Common.Diagnostics;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Approvals.Contracts;
using Unit = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Features.Approvals.Commands.RevokeApproval;

public sealed class RevokeApprovalCommandHandler : IActionHandler<RevokeApprovalCommand, Unit>
{
    private readonly IApprovalStore _store;

    public RevokeApprovalCommandHandler(IApprovalStore store)
    {
        _store = store;
    }

    public async Task<Unit> Handle(RevokeApprovalCommand request, CancellationToken cancellationToken = default)
    {
        var approvalId = request.ApprovalId;
        var by = request.By;
        var ct = cancellationToken;
        var approval = await _store.GetAsync(approvalId, ct) ?? throw new SynentraApplicationException(ApplicationError.Approval.ApprovalNotFound(approvalId));

        if (by.Type != PrincipalType.Admin && by.AgentId != approval.RequestingAgentId)
            throw new SynentraApplicationException(ApplicationError.Approval.ApprovalOnlyAdminOrRequestingAgentMayRevoke);

        approval.Revoke();
        await _store.SaveAsync(approval, ct);
        return Unit.Value;
    }
}
