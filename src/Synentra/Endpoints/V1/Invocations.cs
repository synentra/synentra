using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Invocations.Commands.ResumeInvocation;
using Synentra.Application.Features.Invocations.Commands.SubmitInvocation;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Invocations.Queries.GetInvocation;
using Synentra.Application.Features.Invocations.Queries.GetInvocationResult;
using Synentra.Domain.Security;

namespace Synentra.Endpoints.V1;

public sealed class Invocations : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var g = app.MapGroup("/v1/invocations").WithTags("Invocations");
        g.MapPost("", Submit).WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(256 * 1024));
        g.MapGet("/{id}", Get);
        g.MapPost("/{id}/resume", Resume);
    }

    private static async Task<IResult> Submit(InvocationRequest request, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var result = await dispatcher.Dispatch(new SubmitInvocationCommand(request, principal, ApiEndpointSupport.GetIdempotencyKey(context.Request)), ct);
        context.Response.Headers.Location = $"/v1/invocations/{result.InvocationId}";
        if (result.Decision.Outcome == "deny") return Results.Json(result, statusCode: StatusCodes.Status403Forbidden);
        if (result.Decision.Outcome == "review" || result.Execution.Status is "not_started" or "claimed" or "dispatching" or "unknown") return Results.Json(result, statusCode: StatusCodes.Status202Accepted);
        return Results.Ok(result);
    }

    private static async Task<IResult> Get(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var invocation = await dispatcher.Dispatch(new GetInvocationQuery(id), ct);
        if (invocation is null || (principal.Type != PrincipalType.Admin && (invocation.AuthenticatedAgentId != principal.AgentId || invocation.SecurityScope != principal.SecurityScope))) return Results.NotFound();
        var result = await dispatcher.Dispatch(new GetInvocationResultQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Resume(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, IInvocationStore store, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var admission = await dispatcher.Dispatch(new ResumeInvocationCommand(id, principal), ct);
        await store.SaveDecisionAsync(id, new RecordedDecision(admission.Decision.Outcome.ToString().ToLowerInvariant(), admission.Decision.ReasonCodes, admission.Decision.PolicyRevision, admission.Decision.ExpiresAt), ct);
        var result = await dispatcher.Dispatch(new GetInvocationResultQuery(id), ct);
        if (result is null) return Results.NotFound();
        context.Response.Headers.Location = $"/v1/invocations/{id}";
        if (!admission.Admitted) return Results.Json(result, statusCode: StatusCodes.Status403Forbidden);
        return Results.Json(result, statusCode: StatusCodes.Status202Accepted);
    }
}
