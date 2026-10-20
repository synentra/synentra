using Synentra.Domain.Grants;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.TaskGrants.Commands.IssueTaskGrant;
using Synentra.Application.Features.TaskGrants.Commands.RevokeTaskGrant;

namespace Synentra.Application.Features.TaskGrants;

public sealed class TaskGrantService
{
    private readonly IDispatcher _dispatcher;

    public TaskGrantService(IDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task<TaskGrant> IssueAsync(
        CanonicalPrincipal issuer, 
        TaskGrant requested, 
        IReadOnlyDictionary<string, long> issuerCeilings, 
        CancellationToken ct) => _dispatcher.Dispatch(new IssueTaskGrantCommand(issuer, requested, issuerCeilings), ct);

    public Task RevokeAsync(
        string grantId, 
        CanonicalPrincipal by, 
        CancellationToken ct) => _dispatcher.Dispatch(new RevokeTaskGrantCommand(grantId, by), ct);
}
