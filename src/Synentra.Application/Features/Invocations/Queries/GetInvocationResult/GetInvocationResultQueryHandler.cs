using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Invocations.Contracts;

namespace Synentra.Application.Features.Invocations.Queries.GetInvocationResult;

public sealed class GetInvocationResultQueryHandler(
    IInvocationStore invocations, 
    IApprovalStore approvals, 
    IExecutionRecordStore executions, 
    IOutcomeEvidenceStore outcomes) : IActionHandler<GetInvocationResultQuery, InvocationResultDto?>
{
    public async Task<InvocationResultDto?> Handle(GetInvocationResultQuery request, CancellationToken cancellationToken = default)
    {
        var invocation = await invocations.GetAsync(request.InvocationId, cancellationToken);
        if (invocation is null) 
            return null;

        var decision = await invocations.GetDecisionAsync(request.InvocationId, cancellationToken);
        if (decision is null) 
            return null;

        var approval = await approvals.FindByInvocationAsync(request.InvocationId, cancellationToken);
        var execution = await executions.FindByInvocationAsync(request.InvocationId, cancellationToken);
        var evidence = execution is null 
            ? null 
            : (await outcomes.GetAsync(execution.Id, cancellationToken)).OrderByDescending(x => x.ObservedAt).FirstOrDefault();

        return new InvocationResultDto { 
            InvocationId = request.InvocationId, 
            Decision = new DecisionDto(decision.Outcome, decision.ReasonCodes, 
            decision.PolicyRevision), 
            Approval = approval is null ? null : new ApprovalDto(approval.Id, approval.Status.ToString().ToLowerInvariant()), 
            Execution = new ExecutionDto(execution is null ? "not_started" : LifecycleStatusNames.Execution(execution.Status), execution?.UpstreamReference), 
            Outcome = new OutcomeDto(evidence is null ? "not_checked" : LifecycleStatusNames.Outcome(evidence.Status)) 
        };
    }
}
