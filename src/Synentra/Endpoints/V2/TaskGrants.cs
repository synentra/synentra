using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.TaskGrants.Commands.IssueTaskGrant;
using Synentra.Application.Features.TaskGrants.Commands.RevokeTaskGrant;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Application.Features.TaskGrants.Queries.GetTaskGrant;
using Synentra.Domain.Grants;
using Synentra.Domain.Grants.ValueObjects;
using Synentra.Domain.Security;
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
    private static async Task<IResult> Create(TaskGrantRequest request, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var issuer = await context.RequireIssuerAsync(principals, ct);
        if (request.ExpiresAt <= DateTimeOffset.UtcNow)
            return context.BadRequest("GRANT_EXPIRY_MUST_BE_FUTURE", "Grant expiry must be in the future.");

        if (request.OperationIds.Count == 0)
            return context.BadRequest("GRANT_OPERATION_SCOPE_REQUIRED", "At least one operation scope is required.");

        var grant = new TaskGrant(
            Guid.NewGuid().ToString("N"),
            issuer.AgentId,
            request.AgentId,
            issuer.SecurityScope,
            request.OperationIds,
            request.ResourceScopes.Select(x => new ResourceScope(x.Role, x.Type, x.Ids)).ToArray(),
            request.ExpiresAt,
            request.ExternalReference);

        var effective = await dispatcher.Dispatch(new IssueTaskGrantCommand(issuer, grant), ct);
        return Results.Created($"/v2/task-grants/{effective.Id}", ToResponse(effective));
    }
    private static async Task<IResult> Get(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await context.RequirePrincipalAsync(principals, ct);
        var grant = await dispatcher.Dispatch(new GetTaskGrantQuery(id), ct);
        if (grant is null)
            return context.NotFound("GRANT_NOT_FOUND", "The requested grant was not found.");

        if (principal.Type != PrincipalType.Admin && principal.AgentId != grant.AgentId && principal.AgentId != grant.IssuerId)
            return context.NotFound("GRANT_NOT_FOUND", "The requested grant was not found.");

        return Results.Ok(ToResponse(grant));
    }
    private static async Task<IResult> Revoke(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await context.RequirePrincipalAsync(principals, ct);
        await dispatcher.Dispatch(new RevokeTaskGrantCommand(id, principal), ct);
        return Results.NoContent();
    }

    private static TaskGrantResponse ToResponse(TaskGrant g) => new(g.Id, g.State.ToString().ToLowerInvariant(), g.ExpiresAt);
}
