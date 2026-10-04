using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synentra.Infrastructure.Persistence.Common.Services;
using Synentra.Infrastructure.Persistence.Sqlite.Contexts;

namespace Synentra.Infrastructure.Persistence.Sqlite.Services;

public class SqliteDatabaseInitializer : BaseDatabaseInitializer<SqliteApplicationContext>
{
    public SqliteDatabaseInitializer(
        IDbContextFactory<SqliteApplicationContext> contextFactory,
        ILogger<SqliteDatabaseInitializer> logger)
        : base(contextFactory, logger)
    {
    }

    protected override string GetProviderName() => "SQLite";
}