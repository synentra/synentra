using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Serialization;
using Synentra.Persistence.Common;
using System.Text.Json;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Domain.Grants;

namespace Synentra.Persistence.Features.TaskGrants;

public sealed class GrantStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    PayloadCodec codec, 
    IUnitOfWork work) : IGrantStore
{
    private TaskGrant Map(GrantEntity r) => AggregateReader.Grant(codec.Decode<JsonElement>(r.Payload));

    public async Task<TaskGrant?> GetAsync(string id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var r = await db.Grants.FindAsync(new object[] { id }, ct);
        return r is null ? null : Map(r);
    }

    public async Task SaveAsync(TaskGrant value, CancellationToken ct)
    {
        await work.ExecuteAsync(async token =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(token);
            var r = await db.Grants.FindAsync(new object[] { value.Id.ToString() }, token);
            if (r is null)
            {
                r = new GrantEntity
                {
                    Id = value.Id.ToString()
                };
                db.Add(r);
            }

            r.AgentId = value.AgentId;
            r.SecurityScope = value.SecurityScope;
            r.State = value.State.ToString();
            r.ExpiresAt = value.ExpiresAt.ToUnixTimeMilliseconds();
            r.Payload = codec.Encode(value);
            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }

    public async Task<IReadOnlyList<TaskGrant>> FindActiveForAgentAsync(string agentId, string securityScope, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = Keys.Now;

        return (await db.Grants
            .Where(x => x.AgentId == agentId && x.SecurityScope == securityScope && x.State == "Active" && x.ExpiresAt > now)
            .OrderBy(x => x.Id)
            .ToListAsync(ct))
            .Select(Map)
            .ToList();
    }
}
