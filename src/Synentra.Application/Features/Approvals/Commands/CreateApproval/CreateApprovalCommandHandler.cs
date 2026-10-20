using Synentra.Domain.Approvals;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Application.Features.Auditing.Contracts;

namespace Synentra.Application.Features.Approvals.Commands.CreateApproval;

public sealed class CreateApprovalCommandHandler : IActionHandler<CreateApprovalCommand, Approval>
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);
    private readonly IApprovalStore _store;
    private readonly IAuditStore _audit;

    public CreateApprovalCommandHandler(IApprovalStore store, IAuditStore audit)
    {
        _store = store;
        _audit = audit;
    }

    public async Task<Approval> Handle(CreateApprovalCommand request, CancellationToken cancellationToken = default)
    {
        var invocation = request.Invocation;
        var policyRevision = request.PolicyRevision;
        var reviewObligationRevision = request.ReviewObligationRevision;
        var ct = cancellationToken;

        var approval = new Approval(
            Guid.NewGuid().ToString("N"), 
            invocation.Id, 
            invocation.Digest, 
            invocation.AuthenticatedAgentId, 
            invocation.OperationId.Value, 
            invocation.BindingId, 
            invocation.TaskGrantId, 
            policyRevision, 
            reviewObligationRevision, 
            invocation.SecurityScope, 
            DateTimeOffset.UtcNow + DefaultTtl);

        await _store.SaveAsync(approval, ct);

        await _audit.AppendAsync(
            new AuditEntry(
                Guid.NewGuid().ToString("N"), 
                "approval-created", 
                invocation.AuthenticatedAgentId, 
                invocation.SecurityScope, 
                invocation.OperationId.Value, 
                invocation.BindingId, 
                invocation.TargetId, 
                invocation.Id.ToString(), 
                invocation.Digest.Value, 
                policyRevision, 
                approval.Id, 
                new Dictionary<string, string> { ["expiresAt"] = approval.ExpiresAt.ToString("O") }, 
                DateTimeOffset.UtcNow), 
            ct);

        return approval;
    }
}
