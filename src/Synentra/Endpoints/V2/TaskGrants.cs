using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.TaskGrants.Commands.IssueTaskGrant;
using Synentra.Application.Features.TaskGrants.Commands.RevokeTaskGrant;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Application.Features.TaskGrants.Queries.GetTaskGrant;
using Synentra.Domain.Grants;
using Synentra.Domain.Grants.ValueObjects;
using Synentra.Domain.Security;

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
        var issuer = await ApiEndpointSupport.RequireIssuerAsync(context, principals, ct);
        if (request.ExpiresAt <= DateTimeOffset.UtcNow)
            return Results.BadRequest(new { errorCode = "GRANT_EXPIRY_MUST_BE_FUTURE" });

        if (request.OperationIds.Count == 0)
            return Results.BadRequest(new { errorCode = "GRANT_OPERATION_SCOPE_REQUIRED" });

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
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var grant = await dispatcher.Dispatch(new GetTaskGrantQuery(id), ct);
        if (grant is null)
            return Results.NotFound();

        if (principal.Type != PrincipalType.Admin && principal.AgentId != grant.AgentId && principal.AgentId != grant.IssuerId)
            return Results.NotFound();

        return Results.Ok(ToResponse(grant));
    }
    private static async Task<IResult> Revoke(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        await dispatcher.Dispatch(new RevokeTaskGrantCommand(id, principal), ct);
        return Results.NoContent();
    }

    private static TaskGrantResponse ToResponse(TaskGrant g) => new(g.Id, g.State.ToString().ToLowerInvariant(), g.ExpiresAt);
}
