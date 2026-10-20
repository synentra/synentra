using Synentra.Domain.Execution.Evidence;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Executions.Commands.ReconcileExecution;
using Synentra.Application.Features.Executions.Contracts;

namespace Synentra.Application.Features.Executions.Reconciliation;

public sealed class ReconciliationService
{
    private readonly IDispatcher _dispatcher;
    public ReconciliationService(IDispatcher dispatcher) => _dispatcher = dispatcher;
    public Task<OutcomeEvidence> ReconcileAsync(
        string executionId, 
        VerifierRegistration registration, 
        CanonicalPrincipal operatorPrincipal, 
        CancellationToken ct) 
        => _dispatcher.Dispatch(new ReconcileExecutionCommand(executionId, registration, operatorPrincipal), ct);
}
