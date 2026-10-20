using Synentra.Domain.Grants;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.TaskGrants.Contracts;

namespace Synentra.Application.Features.TaskGrants.Queries.GetTaskGrant;

public sealed class GetTaskGrantQueryHandler : IActionHandler<GetTaskGrantQuery, TaskGrant?>
{
    private readonly IGrantStore _store;

    public GetTaskGrantQueryHandler(IGrantStore store)
    {
        _store = store;
    }

    public async Task<TaskGrant?> Handle(GetTaskGrantQuery request, CancellationToken cancellationToken = default)
    {
        var grantId = request.GrantId;
        var ct = cancellationToken;
        return await _store.GetAsync(grantId, ct);
    }
}
