using Synentra.Domain.Execution.Evidence;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Executions.Contracts;

namespace Synentra.Application.Features.Executions.Commands.ReconcileExecution;

public sealed record ReconcileExecutionCommand(
    string ExecutionId, 
    VerifierRegistration Registration, 
    CanonicalPrincipal OperatorPrincipal) : ICommand<OutcomeEvidence>;
