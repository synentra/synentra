using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Security;
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
        var approval = await _store.GetAsync(approvalId, ct) ?? throw new SynentraApplicationException(ApprovalDiagnostics.ApprovalNotFound(approvalId));

        if (by.Type != PrincipalType.Admin && by.AgentId != approval.RequestingAgentId)
            throw new SynentraApplicationException(ApprovalDiagnostics.ApprovalOnlyAdminOrRequestingAgentMayRevoke);

        approval.Revoke();
        await _store.SaveAsync(approval, ct);
        return Unit.Value;
    }
}
