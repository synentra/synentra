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

        var executionId = Guid.NewGuid().ToString("N");

        try
        {
            // Acquire durable claim (single-winner across replicas)
            var claimId = await _claims.TryAcquireAsync(invocation.Id.ToString(), invocation.AuthenticatedAgentId, DefaultClaimTtl, ct);
            if (claimId is null)
            {
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

            return AdmissionResult.CreateAdmitted(claimId, decision);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Admission failed");
            throw;
        }
    }
}
