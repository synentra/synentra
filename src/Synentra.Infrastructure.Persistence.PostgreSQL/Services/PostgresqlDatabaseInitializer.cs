using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synentra.Infrastructure.Persistence.Common.Services;
using Synentra.Infrastructure.Persistence.PostgreSQL.Contexts;

namespace Synentra.Infrastructure.Persistence.PostgreSQL.Services;

public class PostgresqlDatabaseInitializer : BaseDatabaseInitializer<PostgresqlApplicationContext>
{
    public PostgresqlDatabaseInitializer(
        IDbContextFactory<PostgresqlApplicationContext> contextFactory,
        ILogger<PostgresqlDatabaseInitializer> logger)
        : base(contextFactory, logger)
    {
    }

    protected override string GetProviderName() => "PostgreSQL";
}
