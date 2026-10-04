using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synentra.Infrastructure.Persistence.Common.Exceptions;

namespace Synentra.Infrastructure.Persistence.Common.Services;

/// <summary>
/// Generic base database initializer shared across all persistence providers.
/// </summary>
/// <typeparam name="TDbContext">The DbContext type to use</typeparam>
public abstract class BaseDatabaseInitializer<TDbContext> : IDatabaseInitializer
    where TDbContext : BaseDbContext
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
            throw new DatabaseInitializerException(ex);
        }
    }

    /// <summary>
    /// Override to provide database provider-specific name for logging.
    /// </summary>
    protected abstract string GetProviderName();
}
