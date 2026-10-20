using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Synentra.Persistence.Sqlite;

public static class DependencyInjection
{
    public static IServiceCollection AddSynentraSqlite(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddScoped<IDatabaseInitializer, SqliteDatabaseInitializer>();
        services.AddPooledDbContextFactory<SqliteSynentraDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<IDbContextFactory<SynentraDbContext>>(sp =>
            new SynentraDbContextFactoryAdapter<SqliteSynentraDbContext>(
                sp.GetRequiredService<IDbContextFactory<SqliteSynentraDbContext>>()));

        return services;
    }
}
