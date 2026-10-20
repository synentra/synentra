using Synentra.Domain.Invocations;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Application.Features.Executions.Contracts;

public interface IExecutionSnapshotFactory
{
    Task<AdmittedSnapshot> CreateAsync(
        ToolInvocation invocation, 
        IReadOnlyList<ResourceDescriptor> resources, 
        CancellationToken ct);
}
