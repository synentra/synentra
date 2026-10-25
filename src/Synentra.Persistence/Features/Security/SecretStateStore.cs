using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Persistence.Common;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Serialization;

namespace Synentra.Persistence.Features.Security;

public readonly record struct SecretState(long Generation, bool Revoked);

public sealed class SecretStateStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    IUnitOfWork work, 
    PayloadCodec codec)
{
    public static string Key(SecretProviderRef reference) => Keys.Of(reference.ProviderId, reference.SecretPath, reference.Version);
    public async Task<SecretState> GetAsync(SecretProviderRef reference, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var id = Key(reference);
        var row = await db.SecretStates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? new(0, false) : new(row.Generation, row.Revoked);
    }

    public Task<long> ChangeAsync(
        SecretProviderRef reference, 
        long expectedGeneration, 
        bool revoke, 
        string actorId, 
        CancellationToken ct)
        => work.ExecuteAsync(async token =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(token);
            var id = Key(reference);
            var row = await db.SecretStates.SingleOrDefaultAsync(x => x.Id == id, token);
            if ((row?.Generation ?? 0) != expectedGeneration || row?.Revoked == true) 
                throw new InvalidOperationException("SECRET_GENERATION_CONFLICT");

            if (row is null) { row = new() { Id = id }; db.Add(row); }
            row.Generation = checked(row.Generation + 1); row.Revoked = revoke;
            db.Add(new AuditEntity 
            {
                Id = Guid.NewGuid().ToString("N"), 
                ActorId = actorId, 
                SecurityScope = "secret-administration",
                Timestamp = Keys.Now, 
                Category = revoke ? "SecretAccessRevoked" : "SecretCacheInvalidated",
                Payload = codec.Encode(new { secretId = id, generation = row.Generation })
            });

            return row.Generation;
        }, ct: ct);
}
