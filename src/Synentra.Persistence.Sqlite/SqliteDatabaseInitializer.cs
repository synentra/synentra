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

    protected override string GetProviderName() => "SQLite";
}