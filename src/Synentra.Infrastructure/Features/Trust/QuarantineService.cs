using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Domain.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Persistence;
using Synentra.Persistence.Common;
using Synentra.Persistence.Entities;

namespace Synentra.Infrastructure.Features.Trust;

public sealed class QuarantineService(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    IUnitOfWork work)
{
    public Task<string> QuarantineAsync(
        string agentId, 
        string scope, 
        string reason, 
        CanonicalPrincipal actor, 
        CancellationToken ct) => work.ExecuteAsync(async token =>
    {
        await using var db = await dbFactory.CreateDbContextAsync(token);
        if (actor.Type != PrincipalType.Admin || actor.SecurityScope != scope)
            throw new UnauthorizedAccessException();

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var old = await db.Set<QuarantineEntity>().SingleOrDefaultAsync(x => x.AgentId == agentId && x.SecurityScope == scope && x.ReleasedAt == null, token);
        if (old is not null)
            return old.Id;

        var row = new QuarantineEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            AgentId = agentId,
            SecurityScope = scope,
            Reason = reason,
            StartedAt = Keys.Now
        };
        db.Add(row);
        await db.SaveChangesAsync(token);
        return row.Id;
    }, ct: ct);
    public async Task ReleaseAsync(string id, CanonicalPrincipal actor, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await work.ExecuteAsync(async token =>
        {
            var row = await db.Set<QuarantineEntity>().SingleAsync(x => x.Id == id, token);
            if (actor.Type != PrincipalType.Admin || actor.SecurityScope != row.SecurityScope)
                throw new UnauthorizedAccessException();

            if (row.ReleasedAt is null)
            {
                row.ReleasedAt = Keys.Now;
                row.ReleasedBy = actor.AgentId;
                await db.SaveChangesAsync(token);
            }

            return true;
        }, ct: ct);
    }
}
