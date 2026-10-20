using Synentra.Domain.Invocations;

namespace Synentra.Application.Features.Invocations.Contracts;

public interface IInvocationStore
{
    Task<ToolInvocation?> GetAsync(string invocationId, CancellationToken ct);
    Task SaveAsync(ToolInvocation invocation, CancellationToken ct);
    Task SaveDecisionAsync(string invocationId, RecordedDecision decision, CancellationToken ct);
    Task<RecordedDecision?> GetDecisionAsync(string invocationId, CancellationToken ct);
}
