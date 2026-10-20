using Synentra.Application.Features.Obligations.Contracts;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Domain.Approvals;
using Synentra.Domain.Governance.Obligations;

namespace Synentra.Infrastructure.Features.Obligations;

public sealed class ReviewObligationHandler(IApprovalStore approvals) : IObligationHandler
{
    public string HandlerId => "review";
    public string Version => "1.0.0";
    public ObligationPhase Phase => ObligationPhase.BeforeDispatch;

    public async Task<ObligationResult> HandleAsync(ObligationContext context, CancellationToken ct)
    {
        var approval = await approvals.FindByInvocationAsync(context.InvocationId.ToString(), ct);
        return approval is not null && 
            approval.Status == ApprovalStatus.Approved && 
            approval.ExpiresAt > DateTimeOffset.UtcNow && 
            context.Snapshot is { } snapshot && 
            snapshot.Digest == approval.BindingDigest && 
            approval.PolicyRevision == context.Decision.PolicyRevision 
                ? ObligationResult.Ok() 
                : ObligationResult.Fail("REVIEW_NOT_SATISFIED");
    }
}
