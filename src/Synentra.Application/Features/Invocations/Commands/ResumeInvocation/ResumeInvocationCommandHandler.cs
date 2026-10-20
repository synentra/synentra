using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Approvals;
using Synentra.Application.Common.Diagnostics;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Application.Features.Invocations.Admission;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Invocations.Evaluation;
using Synentra.Application.Features.Invocations.Exceptions;

namespace Synentra.Application.Features.Invocations.Commands.ResumeInvocation;

public sealed class ResumeInvocationCommandHandler : IActionHandler<ResumeInvocationCommand, AdmissionResult>
{
    private readonly IApprovalStore _approvals;
    private readonly IInvocationStore _invocations;
    private readonly IExecutionAdmission _admission;
    private readonly IInvocationEvaluator _evaluator;

    public ResumeInvocationCommandHandler(
        IApprovalStore approvals, 
        IInvocationStore invocations, 
        IExecutionAdmission admission, 
        IInvocationEvaluator evaluator)
    {
        _approvals = approvals;
        _invocations = invocations;
        _admission = admission;
        _evaluator = evaluator;
    }

    public async Task<AdmissionResult> Handle(
        ResumeInvocationCommand request, 
        CancellationToken cancellationToken = default)
    {
        var invocationId = request.InvocationId;
        var caller = request.Caller;
        var ct = cancellationToken;
        var invocation = await _invocations.GetAsync(invocationId, ct) 
            ?? throw new SynentraApplicationException(ApplicationError.Invocation.InvocationNotFound(invocationId));

        if (invocation.AuthenticatedAgentId != caller.AgentId)
            throw new SynentraApplicationException(ApplicationError.Invocation.CrossAgentResumeDenied);

        var approval = await _approvals.FindByInvocationAsync(invocationId, ct) 
            ?? throw new SynentraApplicationException(ApplicationError.Invocation.NoApprovalBoundToInvocation);

        if (approval.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new AuthorityExpiredException("Approval authority has expired.");

        if (approval.Status != ApprovalStatus.Approved)
            throw new SynentraApplicationException(ApplicationError.Invocation.ApprovalNotInApprovedState(approval.Status.ToString()));

        // Revalidate exact snapshot — never accept replacement arguments
        var fresh = await _evaluator.EvaluateAsync(invocation, ct);
        if (fresh.Outcome == DecisionOutcome.Deny)
            return AdmissionResult.NotAdmitted(fresh);

        if (fresh.Outcome == DecisionOutcome.Review)
        // Approval can satisfy its own review obligation once
        {
            if (approval.BindingDigest != invocation.Digest || 
                approval.SecurityScope != invocation.SecurityScope || 
                approval.PolicyRevision != fresh.PolicyRevision || 
                approval.ExpiresAt <= DateTimeOffset.UtcNow)
                return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "APPROVAL_BINDING_INVALID" }));

            approval.Consume();
            await _approvals.SaveAsync(approval, ct);
            fresh = ExecutionDecision.Allow(fresh.ReasonCodes, fresh.Evidence, fresh.PolicyRevision);
        }

        return await _admission.AdmitAsync(fresh, invocation, ct);
    }
}
