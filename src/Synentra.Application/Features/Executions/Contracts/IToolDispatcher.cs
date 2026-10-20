using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Features.Executions.Contracts;

public interface IToolDispatcher
{
    string AdapterId { get; }
    string AdapterVersion { get; }
    IReadOnlyList<string> SupportedExecutionModels { get; }
    Task<TransportReceipt> DispatchAsync(AdmittedSnapshot snapshot, ExecutionIdentity identity, CancellationToken ct);
}
