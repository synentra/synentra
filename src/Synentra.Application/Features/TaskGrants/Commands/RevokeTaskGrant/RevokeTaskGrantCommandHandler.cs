using Synentra.Application.Common.Diagnostics;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.TaskGrants.Contracts;
using Unit = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Features.TaskGrants.Commands.RevokeTaskGrant;

public sealed class RevokeTaskGrantCommandHandler : IActionHandler<RevokeTaskGrantCommand, Unit>
{
    private readonly IGrantStore _store;
    public RevokeTaskGrantCommandHandler(IGrantStore store)
    {
        _store = store;
    }

    public async Task<Unit> Handle(RevokeTaskGrantCommand request, CancellationToken cancellationToken = default)
    {
        var grantId = request.GrantId;
        var by = request.By;
        var ct = cancellationToken;

        var grant = await _store.GetAsync(grantId, ct) ?? throw new SynentraApplicationException(ApplicationError.TaskGrant.GrantNotFound(grantId));
        if (by.Type != Synentra.Domain.Security.PrincipalType.Admin && by.AgentId != grant.IssuerId)
            throw new SynentraApplicationException(ApplicationError.TaskGrant.OnlyIssuerOrAdminMayRevoke);

        grant.Revoke();
        await _store.SaveAsync(grant, ct);
        return Unit.Value;
    }
}
