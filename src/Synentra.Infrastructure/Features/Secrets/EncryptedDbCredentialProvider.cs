using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Synentra.Application.Common.Security;
using Synentra.Application.Common.Persistence;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Common;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class EncryptedDbCredentialProvider(IDbContextFactory<SynentraDbContext> dbFactory, IDataProtectionProvider provider, IUnitOfWork work) : ICredentialProvider
{
    public string ProviderId => "encrypted-db";

    private readonly IDataProtector protector = provider.CreateProtector("Synentra.Secrets.v1");

    public async Task<ResolvedCredential> ResolveAsync(CredentialBinding binding, CancellationToken ct)
    {
        if (binding.SecretRef.ProviderId != ProviderId)
            throw new InvalidOperationException("Credential provider mismatch.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var id = Keys.Of(binding.SecretRef.SecretPath, binding.SecretRef.Version);

        var row = await db.Set<SecretEntity>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) 
            ?? throw new Exception("Secret version not found.");

        if (row.ExpiresAt is { } expiry && expiry <= Keys.Now)
            throw new Exception("Secret expired.");

        return new(row.Type, protector.Unprotect(row.Ciphertext), row.ExpiresAt is { } t ? DateTimeOffset.FromUnixTimeMilliseconds(t) : null, row.Type);
    }

    public async Task StoreVersionAsync(string path, string version, string scheme, string secret, DateTimeOffset? expiresAt, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await work.ExecuteAsync(async token =>
        {
            var id = Keys.Of(path, version);
            if (await db.Set<SecretEntity>().AnyAsync(x => x.Id == id, token))
                throw new InvalidOperationException("Secret versions are immutable.");

            db.Add(new SecretEntity { 
                Id = id,
                Path = path,
                SecretVersion = version,
                Type = scheme,
                Ciphertext = protector.Protect(secret),
                ExpiresAt = expiresAt?.ToUnixTimeMilliseconds() });

            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }
}
