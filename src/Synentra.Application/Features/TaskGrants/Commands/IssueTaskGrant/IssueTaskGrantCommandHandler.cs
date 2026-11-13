using Synentra.Domain.Grants;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.TaskGrants.Contracts;

namespace Synentra.Application.Features.TaskGrants.Commands.IssueTaskGrant;

public sealed class IssueTaskGrantCommandHandler : IActionHandler<IssueTaskGrantCommand, TaskGrant>
{
    private readonly IGrantStore _store;

    public IssueTaskGrantCommandHandler(IGrantStore store)
    {
        _store = store;
    }

    public async Task<TaskGrant> Handle(IssueTaskGrantCommand request, CancellationToken cancellationToken = default)
    {
        var issuer = request.Issuer;
        var requested = request.Requested;
        var ct = cancellationToken;

        var effective = new TaskGrant(
            requested.Id, 
            issuer.AgentId, 
            requested.AgentId, 
            requested.SecurityScope, 
            requested.OperationIds, 
            requested.ResourceScopes,
            requested.ExpiresAt, 
            requested.ExternalReference);

        await _store.SaveAsync(effective, ct);
        return effective;
    }
}
