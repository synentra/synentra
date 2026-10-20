using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.TaskGrants.Commands.IssueTaskGrant;
using Synentra.Application.Features.TaskGrants.Commands.RevokeTaskGrant;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Application.Features.TaskGrants.Queries.GetTaskGrant;
using Synentra.Application.Features.Budgets.Contracts;
using Synentra.Domain.Grants;
using Synentra.Domain.Grants.ValueObjects;
using Synentra.Domain.Security;
using System.Text.Json;

namespace Synentra.Endpoints.V1;

public sealed class TaskGrants : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var g = app.MapGroup("/v1/task-grants").WithTags("Task Grants");
        g.MapPost("", Create);
        g.MapGet("/{id}", Get);
        g.MapPost("/{id}/revoke", Revoke);
    }
    private static async Task<IResult> Create(TaskGrantRequest request, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var issuer = await ApiEndpointSupport.RequireIssuerAsync(context, principals, ct);
        if (request.ExpiresAt <= DateTimeOffset.UtcNow) return Results.BadRequest(new { errorCode = "GRANT_EXPIRY_MUST_BE_FUTURE" });
        if (request.OperationIds.Count == 0) return Results.BadRequest(new { errorCode = "GRANT_OPERATION_SCOPE_REQUIRED" });
        var grant = new TaskGrant(Guid.NewGuid().ToString("N"), issuer.AgentId, request.AgentId, issuer.SecurityScope, request.OperationIds, request.ResourceScopes.Select(x => new ResourceScope(x.Role, x.Type, x.Ids)).ToArray(), request.Limits.Select(x => new GrantLimit(x.Measure, x.Unit, x.Scale, x.PerInvocation, x.Aggregate)).ToArray(), request.ExpiresAt, request.ExternalReference);
        var ceilings = ResolveIssuerCeilings(issuer, request);
        var effective = await dispatcher.Dispatch(new IssueTaskGrantCommand(issuer, grant, ceilings), ct);
        return Results.Created($"/v1/task-grants/{effective.Id}", ToResponse(effective));
    }
    private static async Task<IResult> Get(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, IBudgetStore budgets, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var grant = await dispatcher.Dispatch(new GetTaskGrantQuery(id), ct);
        if (grant is null) return Results.NotFound();
        if (principal.Type != PrincipalType.Admin && principal.AgentId != grant.AgentId && principal.AgentId != grant.IssuerId) return Results.NotFound();
        return Results.Ok(await ToResponseAsync(grant, budgets, ct));
    }
    private static async Task<IResult> Revoke(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        await dispatcher.Dispatch(new RevokeTaskGrantCommand(id, principal), ct);
        return Results.NoContent();
    }
    private static IReadOnlyDictionary<string, long> ResolveIssuerCeilings(Synentra.Domain.Security.ValueObjects.CanonicalPrincipal issuer, TaskGrantRequest request)
    {
        if (issuer.Type == PrincipalType.Admin)
            return request.Limits.ToDictionary(x => $"{x.Measure}:{x.Unit}", x => x.Aggregate ?? x.PerInvocation ?? 0L, StringComparer.Ordinal);

        if (!issuer.Claims.TryGetValue("synentra_grant_limits", out var raw))
            throw new ForbiddenApiException("Trusted issuer has no bounded grant authority claim.");
        Dictionary<string, long>? configured;
        try { configured = JsonSerializer.Deserialize<Dictionary<string, long>>(raw); }
        catch (JsonException) { throw new ForbiddenApiException("Trusted issuer grant authority claim is invalid."); }
        if (configured is null) throw new ForbiddenApiException("Trusted issuer grant authority claim is invalid.");
        return configured;
    }

    private static async Task<TaskGrantResponse> ToResponseAsync(TaskGrant g, IBudgetStore budgets, CancellationToken ct)
    {
        var states = new List<GrantLimitStateDto>(g.Limits.Count);
        foreach (var limit in g.Limits)
        {
            var state = await budgets.GetGrantStateAsync(g.Id, limit.Measure, limit.Unit, limit.Scale, ct);
            var authorized = limit.Aggregate ?? state?.AuthorizedLimit ?? 0L;
            states.Add(new GrantLimitStateDto(limit.Measure, limit.Unit, authorized, state?.Settled ?? 0L, state?.ActiveReserved ?? 0L, state?.Available ?? authorized));
        }
        return new TaskGrantResponse(g.Id, g.State.ToString().ToLowerInvariant(), g.ExpiresAt, states);
    }

    private static TaskGrantResponse ToResponse(TaskGrant g) => new(g.Id, g.State.ToString().ToLowerInvariant(), g.ExpiresAt, g.Limits.Select(x => new GrantLimitStateDto(x.Measure, x.Unit, x.Aggregate ?? 0, 0, 0, x.Aggregate ?? 0)).ToArray());
}
