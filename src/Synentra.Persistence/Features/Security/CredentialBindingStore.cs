using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Serialization;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Persistence.Features.Security;

public sealed class CredentialBindingStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    PayloadCodec codec, 
    IUnitOfWork work)
{
    public async Task<CredentialBinding?> GetAsync(string id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var r = await db.Set<CredentialBindingEntity>().FindAsync(new object[] { id }, ct);
        return r is null ? null : codec.Decode<CredentialBinding>(r.Payload);
    }

    public async Task SaveAsync(CredentialBinding value, CancellationToken ct)
    {
        await work.ExecuteAsync(async token =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(token);
            var r = await db.Set<CredentialBindingEntity>().FindAsync(new object[] { value.Id }, token);
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
            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }
}
