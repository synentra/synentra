using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synentra.Infrastructure.Persistence.Common;
using Synentra.Infrastructure.Persistence.Common.Exceptions;
using Synentra.Infrastructure.Persistence.PostgreSQL.Contexts;

namespace Synentra.Infrastructure.Persistence.PostgreSQL.Services;

public class PostgresqlDatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<PostgresqlApplicationContext> _contextFactory;
    private readonly ILogger<PostgresqlDatabaseInitializer> _logger;

    public PostgresqlDatabaseInitializer(
        IDbContextFactory<PostgresqlApplicationContext> contextFactory,
        ILogger<PostgresqlDatabaseInitializer> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var result = await context.Database.EnsureCreatedAsync(cancellationToken);

            if (result)
                _logger.LogInformation("Application database created successfully (PostgreSQL).");
            else
                _logger.LogInformation("Application database already exists (PostgreSQL).");
        }
        catch (Exception ex)
        {
            throw new DatabaseInitializerException(ex);
        }
    }
}
