using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Domain.Common.Results;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Serialization;

namespace Synentra.Persistence.Features.Auditing;

public sealed class AuditStore(IDbContextFactory<SynentraDbContext> dbFactory, IDbContextScope scope, PayloadCodec codec, IUnitOfWork work) : IAuditStore
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "claimId",
        "expiresAt",
        "reviewer",
        "outcome",
        "reasons",
        "predicateVersion",
        "status",
        "executionId",
        "category",
        "version", "agentId", "delta", "score", "reason", "restrictionId", "evidenceRef"
    };
    public async Task AppendAsync(AuditEntry entry, CancellationToken ct)
    {
        var redacted = entry with
        {
            Details = entry.Details.Where(x => Allowed.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value.Length > 1024 ? x.Value[..1024] : x.Value)
        };
        await work.ExecuteAsync(async token =>
        {
            var db = scope.Current;
            db.Add(new AuditEntity { Id = entry.Id, SecurityScope = entry.SecurityScope, ActorId = entry.ActorId, InvocationId = entry.InvocationId, Category = entry.Category, Timestamp = entry.Timestamp.ToUnixTimeMilliseconds(), Payload = codec.Encode(redacted) });
            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }

    public async Task<Page<AuditEntry>> QueryAsync(AuditQuery query, PageRequest pageRequest, CancellationToken ct)
    {
        if (pageRequest.PageNumber < 0 || pageRequest.PageSize is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(pageRequest));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.Audits.AsNoTracking().AsQueryable();
        if (query.SecurityScope is not null)
            q = q.Where(x => x.SecurityScope == query.SecurityScope);
        if (query.ActorId is not null)
            q = q.Where(x => x.ActorId == query.ActorId);
        if (query.InvocationId is not null)
            q = q.Where(x => x.InvocationId == query.InvocationId);
        if (query.Category is not null)
            q = q.Where(x => x.Category == query.Category);
        if (query.From is { } from)
        {
            var t = from.ToUnixTimeMilliseconds();
            q = q.Where(x => x.Timestamp >= t);
        }

        if (query.To is { } to)
        {
            var t = to.ToUnixTimeMilliseconds();
            q = q.Where(x => x.Timestamp <= t);
        }

        var result = (
            await q
            .OrderBy(x => x.Timestamp)
            .ThenBy(x => x.Id).Skip(pageRequest.PageNumber)
            .Take(pageRequest.PageSize)
            .ToListAsync(ct))
            .Select(x => codec.Decode<AuditEntry>(x.Payload)).ToList();

        var totalCount = await q.CountAsync(ct);
        return new Page<AuditEntry>(result, totalCount);
    }
}
