using Synentra.Domain.Execution.Evidence;
using Synentra.Domain.Execution;

namespace Synentra.Application.Features.Executions.Contracts;

public interface IOutcomeVerifier
{
    string VerifierId { get; }
    string Version { get; }
    Task<OutcomeEvidence> VerifyAsync(ExecutionRecord record, VerifierRegistration registration, CancellationToken ct);
}
