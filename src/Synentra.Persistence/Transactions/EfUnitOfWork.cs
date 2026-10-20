using Microsoft.EntityFrameworkCore;
using System.Data;
using Synentra.Application.Common.Persistence;

namespace Synentra.Persistence.Transactions;

public sealed class EfUnitOfWork(IDbContextFactory<SynentraDbContext> dbFactory) : IUnitOfWork, IDbContextScope
{
    private static readonly AsyncLocal<SynentraDbContext?> CurrentContext = new();

    public SynentraDbContext Current =>
        CurrentContext.Value ?? throw new InvalidOperationException("No active unit-of-work DbContext.");

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, Func<T, bool>? commitWhen = null, CancellationToken ct = default)
    {
        if (CurrentContext.Value is not null)
            return await operation(ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        CurrentContext.Value = db;

        try
        {
            var result = await operation(ct);
            if (commitWhen is null || commitWhen(result))
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            else
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
            }

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            CurrentContext.Value = null;
        }
    }
}
