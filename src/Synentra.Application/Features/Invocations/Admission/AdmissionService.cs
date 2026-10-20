using Synentra.Domain.Effects.ValueObjects;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Common.Persistence;
using Synentra.Domain.Execution;
using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Grants;
using Synentra.Domain.Invocations;
using Microsoft.Extensions.Logging;
using Synentra.Application.Common.Messaging;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Budgets.Contracts;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Executions.Messages;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Application.Features.Trust.Contracts;

namespace Synentra.Application.Features.Invocations.Admission;

public sealed class AdmissionService : IExecutionAdmission
{
    private readonly IUnitOfWork _work;
    private readonly IRevisionProvider _revisions;
    private readonly IExecutionSnapshotFactory _snapshots;
    private static readonly TimeSpan DefaultClaimTtl = TimeSpan.FromSeconds(60);
    private readonly IGrantStore _grants;
    private readonly IApprovalStore _approvals;
    private readonly IBudgetStore _budgets;
    private readonly IExecutionClaimStore _claims;
    private readonly IExecutionRecordStore _records;
    private readonly IInvocationStore _invocations;
    private readonly IAuditStore _audit;
    private readonly IOutbox _outbox;
    private readonly ITrustEngine _trust;
    private readonly ILogger<AdmissionService> _log;

    public AdmissionService(
        IGrantStore grants, 
        IApprovalStore approvals, 
        IBudgetStore budgets, 
        IExecutionClaimStore claims, 
        IExecutionRecordStore records, 
        IInvocationStore invocations, 
        IAuditStore audit, 
        IOutbox outbox, 
        ITrustEngine trust, 
        ILogger<AdmissionService> log, 
        IUnitOfWork work, 
        IExecutionSnapshotFactory snapshots, 
        IRevisionProvider revisions)
    {
        _work = work;
        _snapshots = snapshots;
        _revisions = revisions;
        _grants = grants;
        _approvals = approvals;
        _budgets = budgets;
        _claims = claims;
        _records = records;
        _invocations = invocations;
        _audit = audit;
        _outbox = outbox;
        _trust = trust;
        _log = log;
    }

    public Task<AdmissionResult> AdmitAsync(
        ExecutionDecision decision, 
        ToolInvocation invocation, 
        CancellationToken ct) 
        => _work.ExecuteAsync(token => AdmitCoreAsync(decision, invocation, token), result => result.Admitted, ct);

    private async Task<AdmissionResult> AdmitCoreAsync(
        ExecutionDecision decision, 
        ToolInvocation invocation, 
        CancellationToken ct)
    {
        if (decision.Outcome != DecisionOutcome.Allow)
            return AdmissionResult.NotAdmitted(decision);

        await _revisions.ReloadAsync(ct);
        if (decision.ExpiresAt <= DateTimeOffset.UtcNow || 
            string.IsNullOrEmpty(decision.Evidence.ManifestRevision) || 
            decision.Evidence.ManifestRevision != _revisions.ActiveRevision)
            return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "DECISION_EXPIRED_OR_MANIFEST_CHANGED" }));

        // Recheck quarantine at admission time
        var trust = await _trust.LoadSnapshotAsync(invocation.AuthenticatedAgentId, invocation.SecurityScope, ct);
        if (trust.Quarantined)
            return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "AGENT_QUARANTINED" }));

        // Validate grant if required
        TaskGrant? grant = null;
        if (invocation.RequiresGrant)
        {
            if (invocation.TaskGrantId is null)
                return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "TASK_GRANT_REQUIRED" }));

            grant = await _grants.GetAsync(invocation.TaskGrantId, ct);
            if (grant is null || !grant.IsValidAt(DateTimeOffset.UtcNow) || grant.AgentId != invocation.AuthenticatedAgentId || grant.SecurityScope != invocation.SecurityScope)
                return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "TASK_GRANT_INVALID" }));

            if (!grant.OperationIds.Contains(invocation.OperationId.Value))
                return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "TASK_GRANT_OPERATION_MISMATCH" }));
        }

        if (grant is not null)
        {
            foreach (var resource in decision.Evidence.Resources)
                if (!grant.ResourceScopes.Any(scope => scope.Type == resource.Type && scope.Ids.Contains(resource.Id) && resource.SecurityScope == invocation.SecurityScope && resource.TrustedAttributes.GetValueOrDefault("role") == scope.Role))
                    return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "TASK_GRANT_RESOURCE_MISMATCH" }));

            foreach (var quantities in (decision.Evidence.Effect?.Quantities ?? Array.Empty<EffectQuantity>()).GroupBy(q => (q.Measure, q.Unit, q.Scale)))
            {
                var q = quantities.First();
                var total = quantities.Aggregate(0L, (sum, item) => checked(sum + item.UpperBound));
                var limit = grant.Limits.SingleOrDefault(l => l.Measure == q.Measure && l.Unit == q.Unit && l.Scale == q.Scale);

                if (limit is null || (limit.PerInvocation is { } max && total > max))
                    return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "TASK_GRANT_LIMIT_EXCEEDED" }));
            }
        }

        var executionId = Guid.NewGuid().ToString("N");
        EffectPlan? effectPlan = decision.Evidence.Effect;
        var reservations = new List<IBudgetLease>();

        try
        {
            if (effectPlan is not null)
            {
                if (effectPlan.HasUnknownUpperBound || effectPlan.Quantities.GroupBy(q => (q.Measure, q.Unit, q.Scale, q.BudgetIdentity)).Any(g => g.Count() > 1))
                    return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "INVALID_EFFECT_BUDGET_PLAN" }));

                foreach (var qty in effectPlan.Quantities)
                {
                    var key = new BudgetKey(qty.Measure, qty.Unit, qty.Scale, invocation.SecurityScope, qty.BudgetIdentity);
                    var lease = await _budgets.LockAsync(key, ct);
                    if (!lease.CanReserve(qty.UpperBound))
                    {
                        await lease.DisposeAsync();
                        foreach (var r in reservations)
                            await r.DisposeAsync();
                        return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "BUDGET_EXCEEDED" }));
                    }

                    reservations.Add(lease);
                }

                if (grant is not null)
                {
                    foreach (var group in effectPlan.Quantities.GroupBy(q => (q.Measure, q.Unit, q.Scale)))
                    {
                        var limit = grant.Limits.Single(l => l.Measure == group.Key.Measure && l.Unit == group.Key.Unit && l.Scale == group.Key.Scale);
                        if (limit.Aggregate is not { } aggregate)
                            continue;

                        var total = group.Aggregate(0L, (sum, q) => checked(sum + q.UpperBound));
                        await using var grantLease = await _budgets.LockGrantAsync(grant.Id, group.Key.Measure, group.Key.Unit, group.Key.Scale, aggregate, ct);
                        if (!grantLease.CanReserve(total))
                            return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "TASK_GRANT_AGGREGATE_EXCEEDED" }));

                        grantLease.Reserve(total, executionId);
                    }
                }

                // All reservations validated — commit
                for (int i = 0; i < effectPlan.Quantities.Count; i++)
                    reservations[i].Reserve(effectPlan.Quantities[i].UpperBound, executionId);
            }

            // Acquire durable claim (single-winner across replicas)
            var claimId = await _claims.TryAcquireAsync(invocation.Id.ToString(), invocation.AuthenticatedAgentId, DefaultClaimTtl, ct);
            if (claimId is null)
            {
                foreach (var r in reservations)
                    await r.DisposeAsync();
                return AdmissionResult.NotAdmitted(ExecutionDecision.Deny(new[] { "CLAIM_CONFLICT" }));
            }

            await _invocations.SaveAsync(invocation, ct);
            // Persist execution record before dispatch
            var executionRecord = new ExecutionRecord(executionId, invocation.Id);
            executionRecord.MarkClaimed();
            await _records.SaveAsync(executionRecord, ct);

            // Persist audit + enqueue dispatch
            await _audit.AppendAsync(
                new AuditEntry(
                    Guid.NewGuid().ToString("N"), 
                    "admission", 
                    invocation.AuthenticatedAgentId, 
                    invocation.SecurityScope, 
                    invocation.OperationId.Value,
                    invocation.BindingId,
                    invocation.TargetId, 
                    invocation.Id.ToString(),
                    invocation.Digest.Value, 
                    decision.PolicyRevision, 
                    null, 
                    new Dictionary<string, string> { ["claimId"] = claimId },
                    DateTimeOffset.UtcNow), 
                ct);

            await _outbox.EnqueueAsync(
                new DispatchCommand(
                    invocation.Id.ToString(), 
                    claimId, 
                    executionRecord.Id,
                    await _snapshots.CreateAsync(invocation, decision.Evidence.Resources, ct), 
                    _revisions.ActiveRevision), 
                ct);

            // Commit reservations
            foreach (var r in reservations)
                await r.DisposeAsync();

            return AdmissionResult.CreateAdmitted(claimId, effectPlan ?? EffectPlan.Empty("none"), decision);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Admission failed");
            foreach (var r in reservations)
                await r.DisposeAsync();
            throw;
        }
    }
}
