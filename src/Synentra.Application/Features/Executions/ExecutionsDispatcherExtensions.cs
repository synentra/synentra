using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Executions.Commands.CancelExecution;
using Synentra.Application.Features.Executions.Commands.ReconcileExecution;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Executions.Queries.GetExecution;
using Synentra.Domain.Execution;
using Synentra.Domain.Execution.Evidence;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Features.Executions;

public static class ExecutionsDispatcherExtensions
{
    public static async Task<ExecutionRecord?> GetExecutions(
        this IDispatcher dispatcher,
        string executionId,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new GetExecutionQuery(executionId), cancellationToken);
    }

    public static async Task<OutcomeEvidence> Reconcile(
        this IDispatcher dispatcher,
        string executionId,
        VerifierRegistration registration,
        CanonicalPrincipal operatorPrincipal,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new ReconcileExecutionCommand(executionId, registration, operatorPrincipal), cancellationToken);
    }

    public static async Task<bool> Cancel(
        this IDispatcher dispatcher,
        string executionId,
        CanonicalPrincipal caller,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new CancelExecutionCommand(executionId, caller), cancellationToken);
    }
}
