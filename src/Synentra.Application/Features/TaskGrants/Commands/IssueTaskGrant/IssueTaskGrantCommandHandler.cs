using Synentra.Domain.Grants;
using Synentra.Application.Common.Diagnostics;
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
        var issuerCeilings = request.IssuerCeilings;
        var ct = cancellationToken;

        // Intersect requested authority with issuer authority
        var limits = requested.Limits.Select(l =>
        {
            var ceiling = issuerCeilings.TryGetValue($"{l.Measure}:{l.Unit}", out var v) ? v : 0L;
            return l with
            {
                Aggregate = l.Aggregate is { } agg ? Math.Min(agg, ceiling) : ceiling,
                PerInvocation = l.PerInvocation is { } pi ? Math.Min(pi, ceiling) : null
            };
        }).ToList();

        if (limits.Any(l => (l.Aggregate ?? 0) <= 0))
            throw new SynentraApplicationException(ApplicationError.TaskGrant.IssuerCannotGrantZeroOrNegativeAuthority);

        var effective = new TaskGrant(
            requested.Id, 
            issuer.AgentId, 
            requested.AgentId, 
            requested.SecurityScope, 
            requested.OperationIds, 
            requested.ResourceScopes, 
            limits, 
            requested.ExpiresAt, 
            requested.ExternalReference);

        await _store.SaveAsync(effective, ct);
        return effective;
    }
}
