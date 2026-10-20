using Synentra.Domain.Grants;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.TaskGrants.Contracts;

namespace Synentra.Application.Features.TaskGrants.Queries.ListActiveTaskGrants;

public sealed class ListActiveTaskGrantsQueryHandler : IActionHandler<ListActiveTaskGrantsQuery, IReadOnlyList<TaskGrant>>
{
    private readonly IGrantStore _store;

    public ListActiveTaskGrantsQueryHandler(IGrantStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<TaskGrant>> Handle(ListActiveTaskGrantsQuery request, CancellationToken cancellationToken = default)
    {
        var agentId = request.AgentId;
        var securityScope = request.SecurityScope;
        var ct = cancellationToken;
        return await _store.FindActiveForAgentAsync(agentId, securityScope, ct);
    }
}
