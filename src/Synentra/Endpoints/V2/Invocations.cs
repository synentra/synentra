using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Domain.Security;
using Synentra.Endpoints.V2.Support;
using Synentra.Application.Features.Invocations;
using Synentra.Endpoints.V2.Diagnostics;
using Synentra.Application.Common.Diagnostics;

namespace Synentra.Endpoints.V2;

public sealed class Invocations : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var g = app.MapGroup("/v2/invocations").WithTags("Invocations");
        g.MapPost("", Submit).WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(256 * 1024));
        g.MapGet("/{id}", Get);
        g.MapPost("/{id}/resume", Resume);
    }

    private static async Task<IResult> Submit(
        InvocationRequest request, 
        HttpContext context, 
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        CancellationToken cancellationToken)
    {
        var principal = await context.RequirePrincipalAsync(principals, cancellationToken);
        var result = await dispatcher.Submit(request, principal, context.Request.GetIdempotencyKey(), cancellationToken);
        context.Response.Headers.Location = $"/v2/invocations/{result.InvocationId}";

        if (result.Decision.Outcome == "deny") 
            return Results.Json(result, statusCode: StatusCodes.Status403Forbidden);

        if (result.Decision.Outcome == "review" || result.Execution.Status is "not_started" or "claimed" or "dispatching" or "unknown") 
            return Results.Json(result, statusCode: StatusCodes.Status202Accepted);

        return Results.Ok(result);
    }

    private static async Task<IResult> Get(
        string id, 
        HttpContext context, 
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        CancellationToken cancellationToken)
    {
        var principal = await context.RequirePrincipalAsync(principals, cancellationToken);
        var invocation = await dispatcher.GetInvocations(id, cancellationToken);
        if (invocation is null 
            || (principal.Type != PrincipalType.Admin && (invocation.AuthenticatedAgentId != principal.AgentId 
            || invocation.SecurityScope != principal.SecurityScope))) 
            return context.NotFound(ApplicationError.Invocation.InvocationNotFound(id));

        var result = await dispatcher.GetInvocationResult(id, cancellationToken);
        return result is null
            ? context.NotFound(ApplicationError.Invocation.InvocationNotFound(id))
            : Results.Ok(result);
    }

    private static async Task<IResult> Resume(
        string id, 
        HttpContext context,
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        IInvocationStore store, 
        CancellationToken cancellationToken)
    {
        var principal = await context.RequirePrincipalAsync(principals, cancellationToken);
        var admission = await dispatcher.Resume(id, principal, cancellationToken);
        await store.SaveDecisionAsync(
            id, 
            new RecordedDecision(admission.Decision.Outcome.ToString().ToLowerInvariant(), 
            admission.Decision.ReasonCodes, 
            admission.Decision.PolicyRevision, 
            admission.Decision.ExpiresAt), cancellationToken);

        var result = await dispatcher.GetInvocationResult(id, cancellationToken);

        if (result is null) 
            return context.NotFound(ApplicationError.Invocation.InvocationNotFound(id));

        context.Response.Headers.Location = $"/v2/invocations/{id}";

        if (!admission.Admitted) 
            return Results.Json(result, statusCode: StatusCodes.Status403Forbidden);

        return Results.Json(result, statusCode: StatusCodes.Status202Accepted);
    }
}
