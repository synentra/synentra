using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Budgets.Contracts;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Invocations.Admission;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Domain.Execution;
using Synentra.Domain.Security;

namespace Synentra.Application.Features.Executions.Commands.CancelExecution;

public sealed class CancelExecutionCommandHandler(
    IExecutionRecordStore executions,
    IInvocationStore invocations,
    IExecutionClaimStore claims, 
    IBudgetStore budgets) : IActionHandler<CancelExecutionCommand, bool>
{
    public async Task<bool> Handle(CancelExecutionCommand request, CancellationToken cancellationToken = default)
    {
        var record = await executions.GetAsync(request.ExecutionId, cancellationToken) 
            ?? throw new KeyNotFoundException("Execution not found.");

        var invocation = await invocations.GetAsync(record.InvocationId.ToString(), cancellationToken) 
            ?? throw new KeyNotFoundException("Invocation not found.");

        if (request.Caller.Type != PrincipalType.Admin && 
            (request.Caller.AgentId != invocation.AuthenticatedAgentId || 
            request.Caller.SecurityScope != invocation.SecurityScope))  
            throw new UnauthorizedAccessException("Execution access denied.");

        if (record.Status is not (ExecutionStatus.NotStarted or ExecutionStatus.Claimed)) 
            return false;

        if (!await claims.CancelBeforeDispatchAsync(invocation.Id.ToString(), cancellationToken)) 
            return false;

        await budgets.ReleaseAllForExecutionAsync(record.Id, "cancelled-before-dispatch", cancellationToken);
        record.MarkCancelledBeforeDispatch();
        await executions.SaveAsync(record, cancellationToken);

        return true;
    }
}
