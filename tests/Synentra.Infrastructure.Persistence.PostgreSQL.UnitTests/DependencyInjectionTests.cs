using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Synentra.Application.Abstractions.Persistence;
using Synentra.BuildingBlocks.Configuration.System;
using Synentra.BuildingBlocks.Configuration.System.Storage.Database;
using Synentra.Infrastructure.Persistence.Common;
using Synentra.Infrastructure.Persistence.PostgreSQL.Contexts;

namespace Synentra.Infrastructure.Persistence.PostgreSQL.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddPostgreSqlPersistenceLayer_RegistersIAgentRepository()
    {
        var services = BuildServices();
        services.Any(d => d.ServiceType == typeof(IAgentRepository)).Should().BeTrue();
    }

    [Fact]
    public void AddPostgreSqlPersistenceLayer_RegistersIAgentHistoryRepository()
    {
        var services = BuildServices();
        services.Any(d => d.ServiceType == typeof(IAgentHistoryRepository)).Should().BeTrue();
    }

    [Fact]
    public void AddPostgreSqlPersistenceLayer_RegistersIAuditRepository()
    {
        var services = BuildServices();
        services.Any(d => d.ServiceType == typeof(IAuditRepository)).Should().BeTrue();
    }

    [Fact]
    public void AddPostgreSqlPersistenceLayer_RegistersIDatabaseInitializer()
    {
        var services = BuildServices();
        services.Any(d => d.ServiceType == typeof(IDatabaseInitializer)).Should().BeTrue();
    }

    [Fact]
    public void AddPostgreSqlPersistenceLayer_RegistersDbContextFactory()
    {
        var services = BuildServices();
        services.Any(d => d.ServiceType == typeof(IDbContextFactory<PostgresqlApplicationContext>)).Should().BeTrue();
    }

    [Fact]
    public void AddPostgreSqlPersistenceLayer_IAgentRepository_IsScoped()
    {
        var services = BuildServices();
        var descriptor = services.First(d => d.ServiceType == typeof(IAgentRepository));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddPostgreSqlPersistenceLayer_IAgentHistoryRepository_IsScoped()
    {
        var services = BuildServices();
        var descriptor = services.First(d => d.ServiceType == typeof(IAgentHistoryRepository));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddPostgreSqlPersistenceLayer_IAuditRepository_IsScoped()
    {
        var services = BuildServices();
        var descriptor = services.First(d => d.ServiceType == typeof(IAuditRepository));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddPostgreSqlPersistenceLayer_IDatabaseInitializer_IsScoped()
    {
        var services = BuildServices();
        var descriptor = services.First(d => d.ServiceType == typeof(IDatabaseInitializer));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddPostgreSqlPersistenceLayer_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        RegisterOptions(services);

        var returned = services.AddPostgreSqlPersistenceLayer();

        returned.Should().BeSameAs(services);
    }

    private static IServiceCollection BuildServices()
    {
        var services = new ServiceCollection();
        RegisterOptions(services);
        services.AddPostgreSqlPersistenceLayer();
        return services;
    }

    private static void RegisterOptions(IServiceCollection services)
    {
        var config = new SystemConfiguration
        {
            Storage = new StorageConfiguration
            {
                Database = new DatabaseConfiguration
                {
                    Providers = new DatabaseProviders
                    {
                        Postgres = new PostgreSqlConfiguration
                        {
                            ConnectionString = "Host=localhost;Database=test;Username=postgres;Password=password"
                        }
                    }
                }
            }
        };

        services.AddSingleton<IOptions<SystemConfiguration>>(Options.Create(config));
        services.AddLogging();
    }
}
