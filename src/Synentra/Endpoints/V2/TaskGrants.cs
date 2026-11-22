using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.TaskGrants;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Domain.Grants;
using Synentra.Domain.Grants.ValueObjects;
using Synentra.Domain.Security;
using Synentra.Endpoints.V2.Diagnostics;
using Synentra.Endpoints.V2.Support;

namespace Synentra.Endpoints.V2;

public sealed class TaskGrants : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var g = app.MapGroup("/v2/task-grants").WithTags("Task Grants");
        g.MapPost("", Create);
        g.MapGet("/{id}", Get);
        g.MapPost("/{id}/revoke", Revoke);
    }
    private static async Task<IResult> Create(
        TaskGrantRequest request,
        HttpContext context,
        IDispatcher dispatcher,
        IPrincipalResolver principals,
        CancellationToken cancellationToken)
    {
        var issuer = await context.RequireIssuerAsync(principals, cancellationToken);
        if (request.ExpiresAt <= DateTimeOffset.UtcNow)
            return context.BadRequest(EndpointsError.TaskGrant.GrantExpiryMustBeFuture);

        if (request.OperationIds.Count == 0)
            return context.BadRequest(EndpointsError.TaskGrant.GrantOperationScopeRequired);

        var grant = new TaskGrant(
            Guid.NewGuid().ToString("N"),
            issuer.AgentId,
            request.AgentId,
            issuer.SecurityScope,
            request.OperationIds,
            request.ResourceScopes.Select(x => new ResourceScope(x.Role, x.Type, x.Ids)).ToArray(),
            request.ExpiresAt,
            request.ExternalReference);

        var effective = await dispatcher.IssueTaskGrant(issuer, grant, cancellationToken);
        return Results.Created($"/v2/task-grants/{effective.Id}", ToResponse(effective));
    }

    private static async Task<IResult> Get(
        string id,
        HttpContext context,
        IDispatcher dispatcher,
        IPrincipalResolver principals,
        CancellationToken cancellationToken)
    {
        var principal = await context.RequirePrincipalAsync(principals, cancellationToken);
        var grant = await dispatcher.GetTaskGrant(id, cancellationToken);
        if (grant is null)
            return context.NotFound(EndpointsError.TaskGrant.TaskGrantNotFound);

        if (principal.Type != PrincipalType.Admin && principal.AgentId != grant.AgentId && principal.AgentId != grant.IssuerId)
            return context.NotFound(EndpointsError.TaskGrant.TaskGrantNotFound);

        return Results.Ok(ToResponse(grant));
    }

    private static async Task<IResult> Revoke(
        string id,
        HttpContext context,
        IDispatcher dispatcher,
        IPrincipalResolver principals,
        CancellationToken cancellationToken)
    {
        var principal = await context.RequirePrincipalAsync(principals, cancellationToken);
        await dispatcher.RevokeTaskGrant(id, principal, cancellationToken);
        return Results.NoContent();
    }

    private static TaskGrantResponse ToResponse(TaskGrant g) => new(g.Id, g.State.ToString().ToLowerInvariant(), g.ExpiresAt);
}
