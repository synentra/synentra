using Synentra.Domain.Grants;

namespace Synentra.Application.Features.TaskGrants.Contracts;

public interface IGrantStore
{
    Task<TaskGrant?> GetAsync(string grantId, CancellationToken ct);
    Task SaveAsync(TaskGrant grant, CancellationToken ct);
    Task<IReadOnlyList<TaskGrant>> FindActiveForAgentAsync(string agentId, string securityScope, CancellationToken ct);
}
