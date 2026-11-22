using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Domain.Security;
using Synentra.Endpoints.V2.Support;
using Synentra.Application.Features.Executions;
using Synentra.Endpoints.V2.Diagnostics;
using Synentra.Application.Common.Diagnostics;

namespace Synentra.Endpoints.V2;

public sealed class Executions : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var g = app.MapGroup("/v2/executions").WithTags("Executions");
        g.MapGet("/{id}", Get);
        g.MapPost("/{id}/reconcile", Reconcile);
        g.MapPost("/{id}/cancel", Cancel);
    }

    private static async Task<IResult> Get(
        string id, 
        HttpContext context, 
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        IInvocationStore invocations, 
        IOutcomeEvidenceStore evidence, 
        CancellationToken cancellationToken)
    {
        var principal = await context.RequirePrincipalAsync(principals, cancellationToken);
        var execution = await dispatcher.GetExecutions(id, cancellationToken);

        if (execution is null)
            return context.NotFound(EndpointsError.Execution.ExecutionNotFound);

        var invocation = await invocations.GetAsync(execution.InvocationId.ToString(), cancellationToken);
        if (invocation is null 
            || (principal.Type != PrincipalType.Admin 
            && (principal.AgentId != invocation.AuthenticatedAgentId || principal.SecurityScope != invocation.SecurityScope)))
            return context.NotFound(ApplicationError.Invocation.InvocationNotFound(execution.InvocationId.ToString()));

        var outcomes = await evidence.GetAsync(id, cancellationToken);
        return Results.Ok(new
        {
            id = execution.Id,
            invocationId = execution.InvocationId.ToString(),
            status = LifecycleStatusNames.Execution(execution.Status),
            execution.UpstreamReference,
            execution.DispatchedAt,
            execution.RespondedAt,
            execution.ResponseStatusCode,
            execution.ResponsePayload,
            execution.ErrorMessage,
            outcomeEvidence = outcomes
        });
    }

    private static async Task<IResult> Reconcile(
        string id, 
        HttpContext context, 
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        IInvocationStore invocations, 
        IManifestProvider manifests, 
        IRevisionProvider revisions, 
        CancellationToken cancellationToken)
    {
        var principal = await context.RequireAdminAsync(principals, cancellationToken);
        var execution = await dispatcher.GetExecutions(id, cancellationToken);
        if (execution is null) 
            return context.NotFound(EndpointsError.Execution.ExecutionNotFound);

        var invocation = await invocations.GetAsync(execution.InvocationId.ToString(), cancellationToken);
        if (invocation is null || invocation.SecurityScope != principal.SecurityScope) 
            return context.NotFound(ApplicationError.Invocation.InvocationNotFound(execution.InvocationId.ToString()));

        await revisions.ReloadAsync(cancellationToken);

        var verification = manifests.ActiveBundle?.Spec.Operations.SingleOrDefault(x => x.Id == invocation.OperationId.Value)?.Verification;
        if (verification is null) 
            return context.Conflict(EndpointsError.Execution.VerificationNotConfigured);

        var registration = new VerifierRegistration(
            verification.VerifierRef, 
            verification.Version, 
            verification.Configuration.ToDictionary(x => x.Key, x => x.Value?.ToString() ?? string.Empty, 
            StringComparer.Ordinal));

        var result = await dispatcher.Reconcile(id, registration, principal, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> Cancel(
        string id, 
        HttpContext context, 
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        CancellationToken cancellationToken)
    {
        var principal = await context.RequirePrincipalAsync(principals, cancellationToken);
        var cancelled = await dispatcher.Cancel(id, principal, cancellationToken);
        return cancelled 
            ? Results.Accepted($"/v2/executions/{id}", new { id, status = "cancelled_before_dispatch" }) 
            : context.Conflict("CANCELLATION_UNSUPPORTED_OR_ALREADY_DISPATCHED", "Execution cannot be cancelled in its current state.");
    }
}
