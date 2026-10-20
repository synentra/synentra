using Synentra.Domain.Execution;

namespace Synentra.Application.Features.Executions.Contracts;

public interface IExecutionRecordStore
{
    Task<ExecutionRecord?> GetAsync(string executionId, CancellationToken ct);
    Task SaveAsync(ExecutionRecord record, CancellationToken ct);
    Task<ExecutionRecord?> FindByInvocationAsync(string invocationId, CancellationToken ct);
}
