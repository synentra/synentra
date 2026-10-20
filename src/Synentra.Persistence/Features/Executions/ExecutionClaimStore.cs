using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Common;
using Synentra.Application.Features.Invocations.Admission;

namespace Synentra.Persistence.Features.Executions;

public sealed class ExecutionClaimStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    IUnitOfWork work) : IExecutionClaimStore
{
    public Task<string?> TryAcquireAsync(string invocationId, string claimedBy, TimeSpan ttl, CancellationToken ct) => work.ExecuteAsync<string?>(async token =>
    {
        if (ttl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ttl));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.ExecutionClaims.AnyAsync(x => x.InvocationId == invocationId, token))
            return null;

        var row = new ExecutionClaimEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            InvocationId = invocationId,
            ClaimedBy = claimedBy,
            State = "Claimed",
            ExpiresAt = (DateTimeOffset.UtcNow + ttl).ToUnixTimeMilliseconds()
        };

        db.Add(row);
        await db.SaveChangesAsync(token);
        return row.Id;
    }, ct: ct);

    public Task<bool> TransitionToDispatchingAsync(string claimId, CancellationToken ct) => work.ExecuteAsync(async token =>
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = Keys.Now;

        return await db.ExecutionClaims
                    .Where(x => x.Id == claimId && x.State == "Claimed" && x.ExpiresAt > now)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Dispatching")
                    .SetProperty(x => x.Version, x => x.Version + 1), token) == 1;
    }, ct: ct);

    public async Task MarkRespondedAsync(string claimId, CancellationToken ct)
    {
        await work.ExecuteAsync(async token =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var changed = await db.ExecutionClaims
                                .Where(x => x.Id == claimId && x.State == "Dispatching")
                                .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Responded")
                                .SetProperty(x => x.Version, x => x.Version + 1), token);

            if (changed == 0 && !await db.ExecutionClaims.AnyAsync(x => x.Id == claimId && x.State == "Responded", token))
                throw new InvalidOperationException("Invalid claim transition.");

            return true;
        }, ct: ct);
    }

    public Task<bool> CancelBeforeDispatchAsync(string invocationId, CancellationToken ct) => work.ExecuteAsync(async token =>
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var changed = await db.ExecutionClaims
                    .Where(x => x.InvocationId == invocationId && x.State == "Claimed")
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "CancelledBeforeDispatch")
                    .SetProperty(x => x.Version, x => x.Version + 1), token);

        return changed == 1;
    }, ct: ct);

}
