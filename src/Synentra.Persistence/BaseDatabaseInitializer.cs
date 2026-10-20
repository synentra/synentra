using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// Override to provide database provider-specific name for logging.
    /// </summary>
    protected abstract string GetProviderName();
}