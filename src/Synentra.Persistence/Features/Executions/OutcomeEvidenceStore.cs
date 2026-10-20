using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Serialization;
using System.Text.Json;
using Synentra.Domain.Execution.Evidence;
using Synentra.Application.Features.Executions.Contracts;

namespace Synentra.Persistence.Features.Executions;

public sealed class OutcomeEvidenceStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    PayloadCodec codec, 
    IUnitOfWork work) : IOutcomeEvidenceStore
{
    public async Task AppendAsync(string executionId, OutcomeEvidence evidence, CancellationToken ct)
    {
        await work.ExecuteAsync(async token =>
        {
            if (evidence.ExecutionId != executionId) 
                throw new InvalidOperationException("Evidence execution mismatch.");

            await using var db = await dbFactory.CreateDbContextAsync(token);
            var existing = await db.OutcomeEvidences.FindAsync(new object[] { evidence.Id }, token);
            if (existing is not null)
            {
                if (JsonSerializer.Serialize(codec.Decode<OutcomeEvidence>(existing.Payload), PayloadCodec.Json) != JsonSerializer.Serialize(evidence, PayloadCodec.Json))
                    throw new InvalidOperationException("Evidence identity conflict.");

                return true;
            }

            db.Add(new OutcomeEvidenceEntity { 
                Id = evidence.Id, 
                ExecutionId = executionId, 
                ObservedAt = evidence.ObservedAt.ToUnixTimeMilliseconds(), 
                Payload = codec.Encode(evidence) 
            });

            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }

    public async Task<IReadOnlyList<OutcomeEvidence>> GetAsync(string executionId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return (await db.OutcomeEvidences
                    .Where(x => x.ExecutionId == executionId)
                    .OrderBy(x => x.ObservedAt)
                    .ToListAsync(ct))
            .Select(x => codec.Decode<OutcomeEvidence>(x.Payload))
            .ToList();
    }
}
