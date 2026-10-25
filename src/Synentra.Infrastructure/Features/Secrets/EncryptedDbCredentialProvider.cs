using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Synentra.Application.Common.Security;
using Synentra.Application.Common.Persistence;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Common;
using Synentra.Infrastructure.Diagnostics;
using Synentra.Persistence.Serialization;
using Microsoft.Extensions.Options;
using Synentra.Infrastructure.Configuration.Secrets;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class EncryptedDbCredentialProvider(
    IDbContextFactory<SynentraDbContext> dbFactory,
    IDataProtectionProvider provider, 
    IUnitOfWork work, 
    IOptions<SecretsConfiguration>? options = null) : ICredentialProvider
{
    public string ProviderId => "encrypted-db";

    private void RequireEnabled() 
    { 
        if (options is not null && !options.Value.Providers.EncryptedDatabase.EncryptedDatabaseEnabled) 
            throw new InvalidOperationException("Encrypted database secrets are not enabled."); 
    }

    public async Task<ResolvedCredential> ResolveAsync(CredentialBinding binding, CancellationToken ct)
    {
        RequireEnabled();

        if (binding.SecretRef.ProviderId != ProviderId)
            throw new InvalidOperationException("Credential provider mismatch.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var id = Keys.Of(binding.SecretRef.SecretPath, binding.SecretRef.Version);

        var row = await db.Set<SecretEntity>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) 
            ?? throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialVersionNotFound);

        if (row.ExpiresAt is { } expiry && expiry <= Keys.Now)
            throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialExpired);

        if (!row.Ciphertext.StartsWith("v2:", StringComparison.Ordinal)) 
            throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialRequiresOfflineCiphertextMigration);

        return new(row.Type, Protector(id, row.Type, row.ExpiresAt).Unprotect(row.Ciphertext[3..]), row.ExpiresAt is { } t ? DateTimeOffset.FromUnixTimeMilliseconds(t) : null, row.Type);
    }

    public async Task StoreVersionAsync(
        string path, 
        string version, 
        string scheme, 
        string secret, 
        DateTimeOffset? expiresAt, 
        CancellationToken ct, 
        string actorId = "system")
    {
        RequireEnabled();

        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        if (secret.Any(char.IsControl) || secret.Length > 65536)
            throw new ArgumentException("Secret must be single-line and at most 65536 characters.", nameof(secret));

        if (scheme is not ("Bearer" or "Basic"))
            throw new ArgumentException("Unsupported credential scheme.", nameof(scheme));

        if (expiresAt <= DateTimeOffset.UtcNow) 
            throw new ArgumentException("Expiry must be in the future.");

        await work.ExecuteAsync(async token =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var id = Keys.Of(path, version);
            if (await db.Secrets.AnyAsync(x => x.Id == id, token))
                throw new InvalidOperationException("Secret versions are immutable.");

            db.Add(new SecretEntity { Id = id, Path = path, SecretVersion = version, Type = scheme, Ciphertext = "v2:" + Protector(id, scheme, expiresAt?.ToUnixTimeMilliseconds()).Protect(secret), ExpiresAt = expiresAt?.ToUnixTimeMilliseconds() });

            await Audit("SecretVersionCreated", id, actorId, token);

            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }

    public async Task RevokeVersionAsync(
        string path, 
        string version, 
        CancellationToken ct, 
        string actorId = "system")
    {
        RequireEnabled();

        // A monotonic expiry update keeps an immutable tombstone; a revoked version cannot be recreated.
        await work.ExecuteAsync(async token =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var id = Keys.Of(path, version);
            var count = await db.Secrets.Where(x => x.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, 0L), token);

            if (count != 1) 
                throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialVersionNotFound);

            await Audit("SecretVersionRevoked", id, actorId, token);
            return true;
        }, ct: ct);
    }

    private IDataProtector Protector(
        string id, 
        string scheme, 
        long? expires) => provider.CreateProtector("Synentra.Secrets.v2", id, scheme, expires?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "no-expiry");

    /// <summary>Offline maintenance only. Stop all replicas and back up database and key ring before calling.</summary>
    public Task<int> MigrateLegacyCiphertextAsync(CancellationToken ct) => work.ExecuteAsync(async token =>
    {
        RequireEnabled();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var count = 0;
        while (true)
        {
            var rows = await db.Secrets.Where(x => !x.Ciphertext.StartsWith("v2:")).OrderBy(x => x.Id).Take(100).ToArrayAsync(token);
            if (rows.Length == 0) break;
            foreach (var row in rows)
            {
                if (row.Id != Keys.Of(row.Path, row.SecretVersion)) throw new InvalidDataException("Secret reference identity mismatch.");
                var plaintext = provider.CreateProtector("Synentra.Secrets.v1").Unprotect(row.Ciphertext);
                row.Ciphertext = "v2:" + Protector(row.Id, row.Type, row.ExpiresAt).Protect(plaintext);
                count++;
            }
            await db.SaveChangesAsync(token);
            foreach (var row in rows) db.Entry(row).State = EntityState.Detached;
        }
        await Audit("LegacySecretCiphertextMigrated", count.ToString(System.Globalization.CultureInfo.InvariantCulture), "offline-maintenance", ct);
        return count;
    }, ct: ct);

    private async Task Audit(string category, string id, string actorId, CancellationToken cancellationToken)
    {
        var entry = new AuditEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            ActorId = actorId,
            SecurityScope = "secret-administration",
            Timestamp = Keys.Now,
            Category = category,
            Payload = new PayloadCodec(provider).Encode(new { secretId = id })
        };

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.Add(entry);
    }
}
