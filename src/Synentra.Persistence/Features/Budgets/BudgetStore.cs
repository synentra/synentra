using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Application.Features.Budgets.Contracts;
using Synentra.Persistence.Common;
using Synentra.Persistence.Entities;
using System.Text.Json;

namespace Synentra.Persistence.Features.Budgets;

public sealed class BudgetStore(IDbContextFactory<SynentraDbContext> dbFactory, IDbContextScope scope, IUnitOfWork work) : IBudgetStore
{
    public static string Id(BudgetKey key) => Keys.Of(key.Measure, key.Unit, key.Scale, key.Scope, key.BudgetIdentity);
    // Administrative provisioning only. Never infer authority from the first grant in a scope.
    public async Task SetAuthorizedLimitAsync(BudgetKey key, long limit, CancellationToken ct)
    {
        if (limit < 0 || key.Scale < 0)
            throw new ArgumentOutOfRangeException(nameof(limit));

        await work.ExecuteAsync(async token =>
        {
            var db = scope.Current;
            var id = Id(key);
            var row = await db.BudgetAccounts.FindAsync(new object[] { id }, token);
            if (row is null)
            {
                row = new BudgetAccountEntity
                {
                    Id = id,
                    KeyJson = JsonSerializer.Serialize(key)
                };
                db.Add(row);
            }

            if (checked(row.Settled + row.ActiveReserved) > limit)
                throw new InvalidOperationException("Limit is below committed capacity.");

            row.AuthorizedLimit = limit;
            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }

    public async Task<IBudgetLease> LockAsync(BudgetKey key, CancellationToken ct)
    {
        var db = scope.Current;
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Budget operations require IUnitOfWork.");

        var id = Id(key);
        var account = await db.BudgetAccounts.FindAsync(new object[] { id }, ct) ?? throw new InvalidOperationException("BUDGET_NOT_CONFIGURED");
        var reservations = await db.Reservations.Where(x => x.BudgetId == id).ToListAsync(ct);

        return new Lease(dbFactory, account, reservations);
    }

    public async Task<IBudgetLease> LockGrantAsync(string grantId, string measure, string unit, int scale, long? authorizedLimit, CancellationToken ct)
    {
        var db = scope.Current;
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Grant budgets require IUnitOfWork.");

        var id = Keys.Of("grant", grantId, measure, unit, scale);
        var account = await db.BudgetAccounts.FindAsync(new object[] { id }, ct);
        if (account is null)
        {
            if (authorizedLimit is null or < 0)
                throw new InvalidOperationException("Grant budget not configured.");

            account = new()
            {
                Id = id,
                AuthorizedLimit = authorizedLimit.Value,
                KeyJson = JsonSerializer.Serialize(new { grantId, measure, unit, scale })
            };
            db.Add(account);
        }
        else if (authorizedLimit is not null && account.AuthorizedLimit != authorizedLimit)
            throw new InvalidOperationException("Grant limit cannot change after admission.");

        var reservations = await db.Reservations.Where(x => x.BudgetId == id).ToListAsync(ct);
        return new Lease(dbFactory, account, reservations);
    }


    public async Task<BudgetState?> GetGrantStateAsync(string grantId, string measure, string unit, int scale, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var id = Keys.Of("grant", grantId, measure, unit, scale);
        var account = await db.BudgetAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return account is null ? null : new BudgetState(account.AuthorizedLimit, account.Settled, account.ActiveReserved);
    }


    public Task ReleaseAllForExecutionAsync(string executionId, string evidenceRef, CancellationToken ct) => work.ExecuteAsync(async token =>
    {
        var db = scope.Current;
        var rows = await db.Reservations.Where(x => x.ExecutionId == executionId && !x.ReleaseApplied).ToListAsync(token);
        foreach (var row in rows)
        {
            var account = await db.BudgetAccounts.SingleAsync(x => x.Id == row.BudgetId, token);
            var releasable = checked(row.Reserved - row.Settled);
            if (releasable < 0) 
                throw new InvalidOperationException("Invalid reservation state.");

            if (releasable > 0)
            {
                account.ActiveReserved = checked(account.ActiveReserved - releasable);
                row.Released = releasable;
            }
            row.ReleaseApplied = true;
            row.EvidenceRef = evidenceRef;
        }
        await db.SaveChangesAsync(token);
        return true;
    }, ct: ct);

    private sealed class Lease(IDbContextFactory<SynentraDbContext> dbFactory, 
        BudgetAccountEntity account, List<ReservationEntity> rows) : IBudgetLease
    {
        private bool disposed;
        public long AuthorizedLimit => account.AuthorizedLimit;
        public long Settled => account.Settled;
        public long ActiveReserved => account.ActiveReserved;

        public bool CanReserve(long amount) => amount >= 0 && amount <= AuthorizedLimit - Settled - ActiveReserved;

        private void Check()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
        }

        public async Task Reserve(long amount, string reservationId)
        {
            Check();
            ArgumentException.ThrowIfNullOrWhiteSpace(reservationId);
            var existing = rows.SingleOrDefault(x => x.ExecutionId == reservationId);
            if (existing is not null)
            {
                if (existing.Reserved != amount)
                    throw new InvalidOperationException("Reservation conflict.");

                return;
            }

            if (!CanReserve(amount))
                throw new InvalidOperationException("BUDGET_EXCEEDED");

            var row = new ReservationEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                BudgetId = account.Id,
                ExecutionId = reservationId,
                Reserved = amount
            };
            rows.Add(row);

            await using var db = await dbFactory.CreateDbContextAsync();
            await db.AddAsync(row);
            account.ActiveReserved = checked(account.ActiveReserved + amount);
        }

        public void Settle(long amount, string reservationId)
        {
            Check();
            var row = rows.SingleOrDefault(x => x.ExecutionId == reservationId) 
                ?? throw new InvalidOperationException("Reservation not found.");

            if (row.SettlementApplied)
            {
                if (row.Settled != amount)
                    throw new InvalidOperationException("Settlement conflict.");

                return;
            }

            if (amount < 0 || amount > row.Reserved - row.Released)
                throw new InvalidOperationException("Invalid settlement.");

            row.Settled = amount;
            row.SettlementApplied = true;
            account.Settled = checked(account.Settled + amount);
            account.ActiveReserved -= amount;
        }

        public void Release(long amount, string reservationId, string evidenceRef)
        {
            Check();
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceRef);
            var row = rows.SingleOrDefault(x => x.ExecutionId == reservationId) 
                ?? throw new InvalidOperationException("Reservation not found.");

            if (row.ReleaseApplied)
            {
                if (row.Released != amount || row.EvidenceRef != evidenceRef)
                    throw new InvalidOperationException("Release conflict.");

                return;
            }

            if (amount < 0 || amount > row.Reserved - row.Settled)
                throw new InvalidOperationException("Invalid release.");

            row.Released = amount;
            row.ReleaseApplied = true;
            row.EvidenceRef = evidenceRef;
            account.ActiveReserved -= amount;
        }

        // Disposal never commits; the owning unit of work decides commit versus rollback.
        public ValueTask DisposeAsync()
        {
            disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
