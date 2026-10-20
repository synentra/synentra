using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Executions.Commands.CancelExecution;
using Synentra.Application.Features.Executions.Commands.ReconcileExecution;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Executions.Queries.GetExecution;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Domain.Security;

namespace Synentra.Endpoints.V1;

public sealed class Executions : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var g = app.MapGroup("/v1/executions").WithTags("Executions");
        g.MapGet("/{id}", Get);
        g.MapPost("/{id}/reconcile", Reconcile);
        g.MapPost("/{id}/cancel", Cancel);
    }

    private static async Task<IResult> Get(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, IInvocationStore invocations, IOutcomeEvidenceStore evidence, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var execution = await dispatcher.Dispatch(new GetExecutionQuery(id), ct);
        if (execution is null) return Results.NotFound();
        var invocation = await invocations.GetAsync(execution.InvocationId.ToString(), ct);
        if (invocation is null || (principal.Type != PrincipalType.Admin && (principal.AgentId != invocation.AuthenticatedAgentId || principal.SecurityScope != invocation.SecurityScope))) return Results.NotFound();
        var outcomes = await evidence.GetAsync(id, ct);
        return Results.Ok(new { id = execution.Id, invocationId = execution.InvocationId.ToString(), status = LifecycleStatusNames.Execution(execution.Status), execution.UpstreamReference, execution.DispatchedAt, execution.RespondedAt, execution.ErrorMessage, outcomeEvidence = outcomes });
    }

    private static async Task<IResult> Reconcile(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, IInvocationStore invocations, IManifestProvider manifests, IRevisionProvider revisions, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequireAdminAsync(context, principals, ct);
        var execution = await dispatcher.Dispatch(new GetExecutionQuery(id), ct);
        if (execution is null) return Results.NotFound();
        var invocation = await invocations.GetAsync(execution.InvocationId.ToString(), ct);
        if (invocation is null || invocation.SecurityScope != principal.SecurityScope) return Results.NotFound();
        await revisions.ReloadAsync(ct);
        var verification = manifests.ActiveBundle?.Spec.Operations.SingleOrDefault(x => x.Id == invocation.OperationId.Value)?.Verification;
        if (verification is null) return Results.Conflict(new { errorCode = "VERIFICATION_NOT_CONFIGURED" });
        var registration = new VerifierRegistration(verification.VerifierRef, verification.Version, verification.Configuration.ToDictionary(x => x.Key, x => x.Value?.ToString() ?? string.Empty, StringComparer.Ordinal));
        var result = await dispatcher.Dispatch(new ReconcileExecutionCommand(id, registration, principal), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> Cancel(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var cancelled = await dispatcher.Dispatch(new CancelExecutionCommand(id, principal), ct);
        return cancelled ? Results.Accepted($"/v1/executions/{id}", new { id, status = "cancelled_before_dispatch" }) : Results.Conflict(new { errorCode = "CANCELLATION_UNSUPPORTED_OR_ALREADY_DISPATCHED" });
    }
}
