using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Synentra.Persistence.Sqlite;

public class SqliteDatabaseInitializer : BaseDatabaseInitializer<SqliteSynentraDbContext>
{
    public SqliteDatabaseInitializer(
        IDbContextFactory<SqliteSynentraDbContext> contextFactory,
        ILogger<SqliteDatabaseInitializer> logger)
        : base(contextFactory, logger)
    {
    }

    public override async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
    {
        await base.EnsureDatabaseCreatedAsync(cancellationToken);

        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;", cancellationToken);
    }

    protected override string GetProviderName() => "SQLite";
}