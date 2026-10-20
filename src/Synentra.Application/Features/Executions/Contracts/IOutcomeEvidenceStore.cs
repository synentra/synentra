using Synentra.Domain.Execution.Evidence;

namespace Synentra.Application.Features.Executions.Contracts;

public interface IOutcomeEvidenceStore
{
    Task AppendAsync(string executionId, OutcomeEvidence evidence, CancellationToken ct);
    Task<IReadOnlyList<OutcomeEvidence>> GetAsync(string executionId, CancellationToken ct);
}
