using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Synentra.Persistence.PostgreSql;

public static class DependencyInjection
{
    public static IServiceCollection AddSynentraPostgreSql(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddScoped<IDatabaseInitializer, PostgresqlDatabaseInitializer>();
        services.AddPooledDbContextFactory<PostgreSqlSynentraDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IDbContextFactory<SynentraDbContext>>(sp =>
            new SynentraDbContextFactoryAdapter<PostgreSqlSynentraDbContext>(
                sp.GetRequiredService<IDbContextFactory<PostgreSqlSynentraDbContext>>()));

        return services;
    }
}
