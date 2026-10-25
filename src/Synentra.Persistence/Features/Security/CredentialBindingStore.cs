using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Persistence.Common;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Serialization;

namespace Synentra.Persistence.Features.Security;

public sealed class CredentialBindingStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    PayloadCodec codec, 
    IUnitOfWork work)
{
    public async Task<CredentialBinding?> GetAsync(string id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var r = await db.CredentialBindings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return null;
        var binding = codec.Decode<CredentialBinding>(r.Payload);
        return binding.Revoked ? null : binding;
    }

    public async Task SaveAsync(
        CredentialBinding value,
        string? expectedVersion = null,
        string actorId = "system",
        bool materialOnly = false,
        CancellationToken ct = default)
    {
        await work.ExecuteAsync(async token =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var r = await db.Set<CredentialBindingEntity>().FindAsync(new object[] { value.Id }, token);

            if (string.IsNullOrWhiteSpace(value.Version) 
            || string.IsNullOrWhiteSpace(value.Id) 
            || string.IsNullOrWhiteSpace(value.TargetRef) 
            || string.IsNullOrWhiteSpace(value.AllowedPrincipal) 
            || string.IsNullOrWhiteSpace(value.DelegationScope) 
            || value.AllowedOperations.Count == 0 
            || value.AllowedOperations.Any(string.IsNullOrWhiteSpace) 
            || string.IsNullOrWhiteSpace(value.SecretRef.SecretPath) 
            || string.IsNullOrWhiteSpace(value.SecretRef.Version) 
            || value.SecretRef.ProviderId is not ("encrypted-db" or "hashicorp-vault" or "azure-keyvault" or "aws-secrets"))
                throw new InvalidOperationException("INVALID_CREDENTIAL_BINDING");

            if (r is null && (materialOnly || value.AuthorityVersion != value.Version)) 
                throw new InvalidOperationException("INVALID_AUTHORITY_REVISION");

            if (r is null && expectedVersion is not null) 
                throw new InvalidOperationException("BINDING_VERSION_CONFLICT");

            if (r is not null)
            {
                var previous = codec.Decode<CredentialBinding>(r.Payload);
                if (materialOnly && (!previous.HasSameAuthority(value) || value.Revoked)) 
                    throw new InvalidOperationException("INVALID_MATERIAL_ROTATION");

                if (!materialOnly && value.AuthorityVersion != value.Version) 
                    throw new InvalidOperationException("INVALID_AUTHORITY_REVISION");

                if (previous.Revoked || expectedVersion != previous.Version || value.Version == previous.Version)
                    throw new InvalidOperationException("BINDING_VERSION_CONFLICT");
            }
            if (r is null)
            {
                r = new()
                {
                    Id = value.Id
                };
                db.Add(r);
            }

            r.AgentId = value.AllowedPrincipal;
            r.TargetRef = value.TargetRef;
            r.SecurityScope = value.DelegationScope;
            r.Payload = codec.Encode(value);
            // A unique audit identity also prevents an old revision from being reused (ABA).
            db.Add(new AuditEntity
            {
                Id = Keys.Of("CredentialBindingRevision", value.Id, value.Version),
                ActorId = actorId,
                SecurityScope = value.DelegationScope,
                Timestamp = Keys.Now,
                Category = "CredentialBindingChanged",
                Payload = codec.Encode(new { bindingId = value.Id, revision = value.Version, revoked = value.Revoked })
            });
            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }
}
