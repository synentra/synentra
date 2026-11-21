using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Executions.Commands.CancelExecution;
using Synentra.Application.Features.Invocations.Admission;
using Synentra.Application.Features.Invocations.Commands.ResumeInvocation;
using Synentra.Application.Features.Invocations.Commands.SimulateInvocation;
using Synentra.Application.Features.Invocations.Commands.SubmitInvocation;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Invocations.Queries.GetInvocation;
using Synentra.Application.Features.Invocations.Queries.GetInvocationResult;
using Synentra.Domain.Invocations;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Features.Invocations;

public static class InvocationsDispatcherExtensions
{
    public static async Task<ToolInvocation?> GetInvocations(
        this IDispatcher dispatcher,
        string invocationId,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new GetInvocationQuery(invocationId), cancellationToken);
    }

    public static async Task<InvocationResultDto?> GetInvocationResult(
        this IDispatcher dispatcher,
        string invocationId,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new GetInvocationResultQuery(invocationId), cancellationToken);
    }

    public static async Task<InvocationResultDto> Submit(
        this IDispatcher dispatcher,
        InvocationRequest request,
        CanonicalPrincipal principal,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new SubmitInvocationCommand(request, principal, idempotencyKey), cancellationToken);
    }

    public static async Task<AdmissionResult> Resume(
        this IDispatcher dispatcher,
        string invocationId,
        CanonicalPrincipal caller,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new ResumeInvocationCommand(invocationId, caller), cancellationToken);
    }

    public static async Task<InvocationResultDto> Simulate(
        this IDispatcher dispatcher,
        InvocationRequest request,
        CanonicalPrincipal principal,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new SimulateInvocationCommand(request, principal), cancellationToken);
    }
}
