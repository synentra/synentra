using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Synentra.Application.Common.Serialization;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Approvals;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Invocations.Admission;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Invocations.Evaluation;
using Synentra.Application.Features.Invocations.Normalization;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Execution;

namespace Synentra.Application.Features.Invocations.Commands.SubmitInvocation;

public sealed class SubmitInvocationCommandHandler(
    InvocationNormalizer normalizer,
    IInvocationEvaluator evaluator,
    IExecutionAdmission admission,
    IInvocationStore invocations,
    IIdempotencyStore idempotency,
    IApprovalStore approvals,
    IExecutionRecordStore executions,
    IOutcomeEvidenceStore outcomes,
    ApprovalService approvalService,
    IManifestProvider manifests,
    IRevisionProvider revisions,
    CanonicalJsonService canonical) : IActionHandler<SubmitInvocationCommand, InvocationResultDto>
{
    private static readonly TimeSpan IdempotencyRetention = TimeSpan.FromHours(24);

    public async Task<InvocationResultDto> Handle(SubmitInvocationCommand request, CancellationToken cancellationToken = default)
    {
        await revisions.ReloadAsync(cancellationToken);
        var bindingId = ResolveBinding(request.Request.OperationId, request.Request.BindingId);
        var args = ToDictionary(request.Request.Arguments);
        var context = request.Request.Context is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["workflowId"] = request.Request.Context.WorkflowId ?? string.Empty,
                ["declaredPurpose"] = request.Request.Context.DeclaredPurpose ?? string.Empty
            }.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

        var normalized = await normalizer.NormalizeAsync(request.Modality,
            new NormalizationInput(request.Request.OperationId, bindingId, request.Request.TaskGrantId, args, context, new Dictionary<string, string>(), request.IdempotencyKey, request.Request.Context?.DeclaredPurpose),
            request.Principal, cancellationToken);

        if (normalized.IsMutation && string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new InvalidOperationException("IDEMPOTENCY_KEY_REQUIRED");

        var canonicalInputDigest = ComputeCanonicalInputDigest(normalized.TaskGrantId, canonical.CanonicalizeArgumentDigest(normalized.Arguments));
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await idempotency.GetAsync(normalized.SecurityScope, normalized.AuthenticatedAgentId, normalized.OperationId.Value, request.IdempotencyKey, cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.CanonicalInputDigest, canonicalInputDigest, StringComparison.Ordinal))
                    throw new InvalidOperationException("IDEMPOTENCY_CONFLICT");
                return await BuildResultAsync(existing.InvocationId, cancellationToken);
            }
        }

        await invocations.SaveAsync(normalized, cancellationToken);
        var decision = await evaluator.EvaluateAsync(normalized, cancellationToken);
        await invocations.SaveDecisionAsync(normalized.Id.ToString(), new RecordedDecision(decision.Outcome.ToString().ToLowerInvariant(), decision.ReasonCodes, decision.PolicyRevision, decision.ExpiresAt), cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            await idempotency.RecordAsync(normalized.SecurityScope, normalized.AuthenticatedAgentId, normalized.OperationId.Value, request.IdempotencyKey, canonicalInputDigest, normalized.Digest.Value, normalized.Id.ToString(), DateTimeOffset.UtcNow + IdempotencyRetention, cancellationToken);

        if (decision.Outcome == DecisionOutcome.Review)
        {
            var approval = await approvalService.CreateAsync(normalized, decision.PolicyRevision, decision.ObligationId ?? decision.PolicyRevision, cancellationToken);
            return ToResult(normalized.Id.ToString(), decision, new ApprovalDto(approval.Id, approval.Status.ToString().ToLowerInvariant()), new ExecutionDto("not_started"), new OutcomeDto("not_checked"));
        }

        if (decision.Outcome == DecisionOutcome.Deny)
            return ToResult(normalized.Id.ToString(), decision, null, new ExecutionDto("not_started"), new OutcomeDto("not_checked"));

        var admitted = await admission.AdmitAsync(decision, normalized, cancellationToken);
        if (!admitted.Admitted)
        {
            await invocations.SaveDecisionAsync(normalized.Id.ToString(), new RecordedDecision(admitted.Decision.Outcome.ToString().ToLowerInvariant(), admitted.Decision.ReasonCodes, admitted.Decision.PolicyRevision, admitted.Decision.ExpiresAt), cancellationToken);
            return ToResult(normalized.Id.ToString(), admitted.Decision, null, new ExecutionDto("not_started"), new OutcomeDto("not_checked"));
        }

        var execution = await executions.FindByInvocationAsync(normalized.Id.ToString(), cancellationToken);
        return ToResult(normalized.Id.ToString(), admitted.Decision, null, new ExecutionDto(MapExecution(execution?.Status ?? ExecutionStatus.Claimed), execution?.UpstreamReference), new OutcomeDto("not_checked"));
    }

    private string ResolveBinding(string operationId, string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested)) return requested;
        var candidates = manifests.ActiveBundle?.Spec.Bindings.Where(x => x.OperationId == operationId).Select(x => x.Id).Distinct(StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
        return candidates.Length == 1 ? candidates[0] : throw new InvalidOperationException("AMBIGUOUS_OR_MISSING_BINDING");
    }

    private async Task<InvocationResultDto> BuildResultAsync(string invocationId, CancellationToken ct)
    {
        var decision = await invocations.GetDecisionAsync(invocationId, ct) ?? throw new InvalidOperationException("INVOCATION_DECISION_MISSING");
        var approval = await approvals.FindByInvocationAsync(invocationId, ct);
        var execution = await executions.FindByInvocationAsync(invocationId, ct);
        var outcome = execution is null ? null : (await outcomes.GetAsync(execution.Id, ct)).OrderByDescending(x => x.ObservedAt).FirstOrDefault();
        return new InvocationResultDto
        {
            InvocationId = invocationId,
            Decision = new DecisionDto(decision.Outcome, decision.ReasonCodes, decision.PolicyRevision),
            Approval = approval is null ? null : new ApprovalDto(approval.Id, approval.Status.ToString().ToLowerInvariant()),
            Execution = new ExecutionDto(MapExecution(execution?.Status ?? ExecutionStatus.NotStarted), execution?.UpstreamReference),
            Outcome = new OutcomeDto(outcome?.Status.ToString().ToLowerInvariant() ?? "not_checked")
        };
    }

    private static InvocationResultDto ToResult(string invocationId, ExecutionDecision decision, ApprovalDto? approval, ExecutionDto execution, OutcomeDto outcome) => new()
    {
        InvocationId = invocationId,
        Decision = new DecisionDto(decision.Outcome.ToString().ToLowerInvariant(), decision.ReasonCodes, decision.PolicyRevision),
        Approval = approval,
        Execution = execution,
        Outcome = outcome
    };

    private static string MapExecution(ExecutionStatus status) => LifecycleStatusNames.Execution(status);

    private static string ComputeCanonicalInputDigest(string? grantId, string argumentDigest)
    {
        var bytes = Encoding.UTF8.GetBytes($"v1|{grantId ?? string.Empty}|{argumentDigest}");
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static IReadOnlyDictionary<string, object> ToDictionary(JsonElement element)
        => JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText()) ?? new Dictionary<string, object>();
}
