using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Domain.Security;
using Synentra.Domain.Execution.Evidence;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Executions.Contracts;

namespace Synentra.Application.Features.Executions.Commands.ReconcileExecution;

public sealed class ReconcileExecutionCommandHandler : IActionHandler<ReconcileExecutionCommand, OutcomeEvidence>
{
    private readonly IExecutionRecordStore _records;
    private readonly IOutcomeVerifier _verifier;
    private readonly IAuditStore _audit;
    private readonly IOutcomeEvidenceStore _evidence;
    private readonly IInvocationStore _invocations;

    public ReconcileExecutionCommandHandler(
        IExecutionRecordStore records, 
        IOutcomeVerifier verifier, 
        IAuditStore audit,
        IOutcomeEvidenceStore evidence,
        IInvocationStore invocations)
    {
        _records = records;
        _verifier = verifier;
        _audit = audit; _evidence = evidence; _invocations = invocations;
    }

    public async Task<OutcomeEvidence> Handle(ReconcileExecutionCommand request, CancellationToken cancellationToken = default)
    {
        var executionId = request.ExecutionId;
        var registration = request.Registration;
        var operatorPrincipal = request.OperatorPrincipal;
        var ct = cancellationToken;

        var record = await _records.GetAsync(executionId, ct) 
            ?? throw new InvalidOperationException($"Execution {executionId} not found.");

        var invocation = await _invocations.GetAsync(record.InvocationId.ToString(), ct) 
            ?? throw new InvalidOperationException("Invocation missing.");

        if (operatorPrincipal.Type != PrincipalType.Admin || operatorPrincipal.SecurityScope != invocation.SecurityScope)
            throw new UnauthorizedAccessException();

        if (registration.VerifierRef != _verifier.VerifierId || registration.Version != _verifier.Version)
            throw new InvalidOperationException("Registered verifier is unavailable.");

        // Read back authoritative state without replaying the original mutation.
        var evidence = await _verifier.VerifyAsync(record, registration, ct);
        await _evidence.AppendAsync(executionId, evidence, ct);
        await _audit.AppendAsync(new AuditEntry(Guid.NewGuid().ToString("N"), "reconciliation", operatorPrincipal.AgentId, operatorPrincipal.SecurityScope, null, null, null, executionId, null, null, null, new Dictionary<string, string> { ["predicateVersion"] = registration.Version, ["status"] = evidence.Status.ToString() }, DateTimeOffset.UtcNow), ct);
        return evidence;
    }
}
