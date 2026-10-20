using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Common;
using Synentra.Application.Features.Invocations.Contracts;

namespace Synentra.Persistence.Features.Invocations;

public sealed class IdempotencyStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    IUnitOfWork work) : IIdempotencyStore
{
    private static IdempotencyEntry Map(IdempotencyEntity r) 
        => new(r.Id, r.SecurityScope, 
            r.ActorId, r.OperationId, 
            r.Key, r.CanonicalInputDigest, 
            r.ExactBindingDigest, r.InvocationId, 
            DateTimeOffset.FromUnixTimeMilliseconds(r.ExpiresAt));

    public async Task<IdempotencyEntry?> GetAsync(string securityScope, string actorId, string operationId, string key, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var id = Keys.Of(securityScope, actorId, operationId, key);
        var now = Keys.Now;
        var row = await db.Idempotencies.SingleOrDefaultAsync(x => x.ScopeKey == id && x.ExpiresAt > now, ct);
        return row is null ? null : Map(row);
    }

    public Task<IdempotencyEntry> RecordAsync(string securityScope, string actorId, string operationId, string key, string canonicalInputDigest, string exactBindingDigest, string invocationId, DateTimeOffset expiresAt, CancellationToken ct) => work.ExecuteAsync(async token =>
    {
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAt));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var hash = Keys.Of(securityScope, actorId, operationId, key);
        var row = await db.Idempotencies.SingleOrDefaultAsync(x => x.ScopeKey == hash, token);
        if (row is not null && row.ExpiresAt > Keys.Now)
        {
            if (row.CanonicalInputDigest != canonicalInputDigest)
                throw new InvalidOperationException("IDEMPOTENCY_CONFLICT");

            return Map(row);
        }

        if (row is not null)
        {
            db.Remove(row);
            await db.SaveChangesAsync(token);
        }

        row = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ScopeKey = hash,
            SecurityScope = securityScope,
            ActorId = actorId,
            OperationId = operationId,
            Key = key,
            CanonicalInputDigest = canonicalInputDigest,
            ExactBindingDigest = exactBindingDigest,
            InvocationId = invocationId,
            ExpiresAt = expiresAt.ToUnixTimeMilliseconds()
        };

        db.Add(row);
        await db.SaveChangesAsync(token);
        return Map(row);
    }, ct: ct);
}
