using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Synentra.Persistence.PostgreSql;

public class PostgresqlDatabaseInitializer : BaseDatabaseInitializer<PostgreSqlSynentraDbContext>
{
    public PostgresqlDatabaseInitializer(
        IDbContextFactory<PostgreSqlSynentraDbContext> contextFactory,
        ILogger<PostgresqlDatabaseInitializer> logger)
        : base(contextFactory, logger)
    {
    }

    protected override string GetProviderName() => "PostgreSQL";
}