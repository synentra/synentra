using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Synentra.Application.Abstractions.Persistence;
using Synentra.BuildingBlocks.Configuration.System;
using Synentra.Infrastructure.Persistence.Common;
using Synentra.Infrastructure.Persistence.PostgreSQL.Contexts;
using Synentra.Infrastructure.Persistence.PostgreSQL.Repositories;
using Synentra.Infrastructure.Persistence.PostgreSQL.Services;

namespace Synentra.Infrastructure.Persistence.PostgreSQL;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgreSqlPersistenceLayer(
        this IServiceCollection services)
    {
        services
            .AddScoped<IAgentRepository, AgentRepository>()
            .AddScoped<IAgentHistoryRepository, AgentHistoryRepository>()
            .AddScoped<IAuditRepository, AuditRepository>()
            .AddScoped<IDatabaseInitializer, PostgresqlDatabaseInitializer>();

        services.AddPooledDbContextFactory<PostgresqlApplicationContext>((sp, options) =>
        {
            var db = sp.GetRequiredService<IOptions<SystemConfiguration>>()
                       .Value.Storage.Database;

            options.UseNpgsql(db.Providers.Postgres.ConnectionString);
        });

        return services;
    }
}
