using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synentra.Application.Common.Versioning;

namespace Synentra.Persistence;

public abstract class BaseDatabaseInitializer<TDbContext> : IDatabaseInitializer
    where TDbContext : SynentraDbContext
{
    protected readonly IDbContextFactory<TDbContext> ContextFactory;
    protected readonly ILogger Logger;

    protected BaseDatabaseInitializer(
        IDbContextFactory<TDbContext> contextFactory,
        ILogger logger)
    {
        ContextFactory = contextFactory;
        Logger = logger;
    }

    public virtual async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
            var result = await context.Database.EnsureCreatedAsync(cancellationToken);
            await ValidateCompatibilityAsync(context, cancellationToken);

            var providerName = GetProviderName();
            if (result)
                Logger.LogInformation("Application database created successfully ({ProviderName}).", providerName);
            else
                Logger.LogInformation("Application database already exists ({ProviderName}).", providerName);
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while initializing the database.", ex);
        }
    }

    private static async Task ValidateCompatibilityAsync(TDbContext context, CancellationToken ct)
    {
        await context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS synentra_compatibility (id INTEGER PRIMARY KEY, schema_version INTEGER NOT NULL)", ct);

        await using var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT schema_version FROM synentra_compatibility WHERE id = 1";
        var value = await command.ExecuteScalarAsync(ct);
        if (value is null or DBNull)
        {
            command.CommandText = $"INSERT INTO synentra_compatibility (id, schema_version) VALUES (1, {CompatibilityPolicy.CurrentPersistenceSchemaVersion})";
            await command.ExecuteNonQueryAsync(ct);
            return;
        }

        var version = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        if (!CompatibilityPolicy.IsSupportedPersistenceSchemaVersion(version))
            throw new InvalidOperationException($"Unsupported persistence schema version '{version}'. Expected '{CompatibilityPolicy.CurrentPersistenceSchemaVersion}'.");
    }

    /// <summary>
    /// Override to provide database provider-specific name for logging.
    /// </summary>
    protected abstract string GetProviderName();
}