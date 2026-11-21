using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.TaskGrants.Commands.IssueTaskGrant;
using Synentra.Application.Features.TaskGrants.Commands.RevokeTaskGrant;
using Synentra.Application.Features.TaskGrants.Queries.GetTaskGrant;
using Synentra.Domain.Grants;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Features.TaskGrants;

public static class TaskGrantsDispatcherExtensions
{
    public static async Task<TaskGrant?> GetTaskGrant(
        this IDispatcher dispatcher,
        string grantId,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new GetTaskGrantQuery(grantId), cancellationToken);
    }

    public static async Task<TaskGrant> IssueTaskGrant(
        this IDispatcher dispatcher,
        CanonicalPrincipal issuer,
        TaskGrant requested,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new IssueTaskGrantCommand(issuer, requested), cancellationToken);
    }

    public static async Task RevokeTaskGrant(
        this IDispatcher dispatcher,
        string grantId,
        CanonicalPrincipal canonicalPrincipal,
        CancellationToken cancellationToken)
    {
        await dispatcher.Dispatch(new RevokeTaskGrantCommand(grantId, canonicalPrincipal), cancellationToken);
    }
}
