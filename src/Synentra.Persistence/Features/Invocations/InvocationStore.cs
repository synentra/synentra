using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Serialization;
using System.Text.Json;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Domain.Invocations;

namespace Synentra.Persistence.Features.Invocations;

public sealed class InvocationStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    PayloadCodec codec, 
    IUnitOfWork work) : IInvocationStore
{
    private sealed record PersistedInvocation(ToolInvocation? Invocation, RecordedDecision? Decision);

    private ToolInvocation Map(InvocationEntity row)
    {
        try
        {
            var wrapped = codec.Decode<PersistedInvocation>(row.Payload);
            if (wrapped.Invocation is not null) return wrapped.Invocation;
        }
        catch (JsonException) { }
        return codec.Decode<ToolInvocation>(row.Payload);
    }

    private RecordedDecision? MapDecision(InvocationEntity row)
    {
        try { return codec.Decode<PersistedInvocation>(row.Payload).Decision; }
        catch (JsonException) { return null; }
    }

    public async Task<ToolInvocation?> GetAsync(string id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Invocations.FindAsync(new object[] { id }, ct);
        return row is null ? null : Map(row);
    }

    public async Task SaveAsync(ToolInvocation value, CancellationToken ct)
    {
        await work.ExecuteAsync(async token =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(token);
            var row = await db.Invocations.FindAsync(new object[] { value.Id.ToString() }, token);
            if (row is not null)
            {
                var previous = Map(row);
                if (JsonSerializer.Serialize(previous, PayloadCodec.Json) != JsonSerializer.Serialize(value, PayloadCodec.Json))
                    throw new InvalidOperationException("Invocation snapshots are immutable.");

                return true;
            }

            row = new InvocationEntity { Id = value.Id.ToString() };
            db.Add(row);
            row.SecurityScope = value.SecurityScope;
            row.Digest = value.Digest.Value;
            row.Payload = codec.Encode(new PersistedInvocation(value, null));
            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }

    public async Task SaveDecisionAsync(string invocationId, RecordedDecision decision, CancellationToken ct)
    {
        await work.ExecuteAsync(async token =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(token);
            var row = await db.Invocations.FindAsync(new object[] { invocationId }, token) ?? throw new InvalidOperationException("Invocation missing.");

            var invocation = Map(row);
            row.Payload = codec.Encode(new PersistedInvocation(invocation, decision));
            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }

    public async Task<RecordedDecision?> GetDecisionAsync(string invocationId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Invocations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == invocationId, ct);
        return row is null ? null : MapDecision(row);
    }
}
