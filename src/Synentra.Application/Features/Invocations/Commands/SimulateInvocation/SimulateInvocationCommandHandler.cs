using System.Text.Json;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Invocations.Evaluation;
using Synentra.Application.Features.Invocations.Normalization;
using Synentra.Application.Features.Manifests.Contracts;

namespace Synentra.Application.Features.Invocations.Commands.SimulateInvocation;

public sealed class SimulateInvocationCommandHandler(
    InvocationNormalizer normalizer, 
    IInvocationEvaluator evaluator, 
    IManifestProvider manifests, 
    IRevisionProvider revisions) : IActionHandler<SimulateInvocationCommand, InvocationResultDto>
{
    public async Task<InvocationResultDto> Handle(SimulateInvocationCommand request, CancellationToken cancellationToken = default)
    {
        await revisions.ReloadAsync(cancellationToken);
        var bindingId = request.Request.BindingId;

        if (string.IsNullOrWhiteSpace(bindingId))
        {
            var candidates = manifests.ActiveBundle?.Spec.Bindings
                .Where(x => x.OperationId == request.Request.OperationId)
                .Select(x => x.Id)
                .Distinct(StringComparer.Ordinal)
                .ToArray() ?? Array.Empty<string>();

            bindingId = candidates.Length == 1 
                ? candidates[0] 
                : throw new InvalidOperationException("AMBIGUOUS_OR_MISSING_BINDING");
        }

        var args = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Request.Arguments.GetRawText()) ?? new Dictionary<string, object>();
        var context = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(request.Request.Context?.WorkflowId)) 
            context["workflowId"] = request.Request.Context!.WorkflowId!;

        if (!string.IsNullOrWhiteSpace(request.Request.Context?.DeclaredPurpose)) 
            context["declaredPurpose"] = request.Request.Context!.DeclaredPurpose!;

        var invocation = await normalizer.NormalizeAsync(
            request.Modality, 
            new NormalizationInput(
                request.Request.OperationId, 
                bindingId, request.Request.TaskGrantId, 
                args, 
                context, 
                new Dictionary<string, string>(), 
                null, 
                request.Request.Context?.DeclaredPurpose), 
            request.Principal, 
            cancellationToken);

        var decision = await evaluator.EvaluateAsync(invocation, cancellationToken);

        return new InvocationResultDto
        {
            InvocationId = invocation.Id.ToString(),
            Decision = new DecisionDto(decision.Outcome.ToString().ToLowerInvariant(), decision.ReasonCodes, decision.PolicyRevision),
            Approval = null,
            Execution = new ExecutionDto("not_started"),
            Outcome = new OutcomeDto("not_checked")
        };
    }
}
