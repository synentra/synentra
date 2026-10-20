using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Messaging;
using Synentra.Application.Common.Persistence;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Executions.Messages;
using Synentra.Application.Features.Invocations.Admission;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Trust.Contracts;
using Synentra.Domain.Execution;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Common;
using Synentra.Persistence.Serialization;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Persistence.Features.Security;

namespace Synentra.Infrastructure.Features.Executions;

public sealed class OutboxProcessor(
    IDbContextFactory<SynentraDbContext> dbFactory, IUnitOfWork work, 
    PayloadCodec codec, IExecutionClaimStore claims, IExecutionRecordStore records, 
    IInvocationStore invocations, IRevisionProvider revisions, IManifestProvider manifests, 
    ITrustEngine trust, IGrantStore grants, 
    CredentialBindingStore credentials, IEnumerable<IToolDispatcher> dispatchers) 
    : IOutboxProcessor
{
    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ids = await db.Outboxes.AsNoTracking()
            .Where(x => x.ProcessedAt == null && x.Attempts < 5)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id).Take(20)
            .ToListAsync(ct);

        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            DispatchCommand? command;
            try
            {
                command = await work.ExecuteAsync<DispatchCommand?>(async token =>
                {
                    var message = await db.Set<OutboxEntity>().SingleAsync(x => x.Id == id, token);
                    if (message.ProcessedAt is not null)
                        return null;

                    message.Attempts++;
                    if (message.Type != "dispatch.v1")
                    {
                        message.ProcessedAt = Keys.Now;
                        message.LastError = "UNREGISTERED_MESSAGE";
                        return null;
                    }

                    var cmd = codec.Decode<DispatchCommand>(message.Payload);
                    var invocation = await invocations.GetAsync(cmd.InvocationId, token) 
                        ?? throw new InvalidDataException("Invocation missing.");

                    var record = await records.GetAsync(cmd.ExecutionRecordId, token) 
                        ?? throw new InvalidDataException("Execution missing.");

                    await revisions.ReloadAsync(token);
                    var snapshot = await trust.LoadSnapshotAsync(invocation.AuthenticatedAgentId, invocation.SecurityScope, token);
                    var target = manifests.ActiveBundle?.Spec.Targets.SingleOrDefault(t => t.Id == invocation.TargetId);
                    var adapter = dispatchers.SingleOrDefault(x => x.AdapterId == target?.AdapterRef);

                    var currentCredential = cmd.Snapshot.CredentialBinding is { } binding ? await credentials.GetAsync(binding.Id, token) : null;
                    var credentialChanged = System.Text.Json.JsonSerializer.Serialize(currentCredential, PayloadCodec.Json) != System.Text.Json.JsonSerializer.Serialize(cmd.Snapshot.CredentialBinding, PayloadCodec.Json);

                    var grant = invocation.TaskGrantId is { } grantId ? await grants.GetAsync(grantId, token) : null;
                    var grantInvalid = invocation.RequiresGrant && (grant is null || !grant.IsValidAt(DateTimeOffset.UtcNow) || grant.AgentId != invocation.AuthenticatedAgentId || grant.SecurityScope != invocation.SecurityScope);

                    if (grantInvalid || credentialChanged || snapshot.Quarantined || revisions.ActiveRevision != cmd.ManifestRevision || adapter is null)
                    {
                        record.MarkFailedBeforeDispatch("DISPATCH_REVALIDATION_FAILED");
                        await records.SaveAsync(record, token);
                        message.ProcessedAt = Keys.Now;
                        message.LastError = "DISPATCH_REVALIDATION_FAILED";
                        return null;
                    }

                    if (!await claims.TransitionToDispatchingAsync(cmd.ClaimId, token))
                    {
                        if (record.Status == ExecutionStatus.NotStarted)
                        {
                            record.MarkFailedBeforeDispatch("CLAIM_EXPIRED_OR_CONFLICT");
                            await records.SaveAsync(record, token);
                        }

                        message.ProcessedAt = Keys.Now;
                        return null;
                    }

                    record.MarkDispatching();
                    await records.SaveAsync(record, token);
                    // Persist dispatch ownership BEFORE external I/O. Never automatically redeliver after this boundary.
                    message.ProcessedAt = Keys.Now;
                    return cmd;
                }, ct: ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // The failed transaction rolled back its attempt count. Persist a bounded retry/dead-letter record separately.
                await work.ExecuteAsync(async token =>
                {
                    var message = await db.Outboxes.SingleAsync(x => x.Id == id, token);
                    if (message.ProcessedAt is null)
                    {
                        message.Attempts++;
                        message.LastError = ex.GetType().Name;
                    }

                    return true;
                }, ct: ct);
                continue;
            }

            if (command is null)
                continue;
            var invocationForDispatch = await invocations.GetAsync(command.InvocationId, ct) 
                ?? throw new InvalidDataException("Invocation missing.");

            var targetForDispatch = manifests.ActiveBundle!.Spec.Targets.Single(t => t.Id == invocationForDispatch.TargetId);
            var dispatcher = dispatchers.Single(x => x.AdapterId == targetForDispatch.AdapterRef);
            TransportReceipt receipt;
            try
            {
                receipt = await dispatcher.DispatchAsync(command.Snapshot, new ExecutionIdentity(invocationForDispatch.AuthenticatedAgentId, invocationForDispatch.SecurityScope, invocationForDispatch.AuthenticatedAgentId, null, null), ct);
            }
            catch
            {
                receipt = TransportReceipt.Unknown("DISPATCH_UNCERTAIN");
            }

            // Cancellation after I/O must not prevent durable uncertainty recording.
            using var persistTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await work.ExecuteAsync(async token =>
            {
                var record = await records.GetAsync(command.ExecutionRecordId, token) 
                    ?? throw new InvalidDataException("Execution missing.");

                if (receipt.Status is ReceiptStatus.Responded or ReceiptStatus.ToolError)
                {
                    record.MarkResponded(receipt.UpstreamReference);
                    await claims.MarkRespondedAsync(command.ClaimId, token);
                }
                else
                {
                    record.MarkUnknown("DISPATCH_UNCERTAIN");
                    await db.ExecutionClaims
                        .Where(x => x.Id == command.ClaimId && x.State == "Dispatching")
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Unknown")
                        .SetProperty(x => x.Version, x => x.Version + 1), token);
                }

                await records.SaveAsync(record, token);
                return true;
            }, ct: persistTimeout.Token);
        }
    }
}
