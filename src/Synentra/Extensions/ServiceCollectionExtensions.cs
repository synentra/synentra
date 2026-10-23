using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Synentra.Application.Common.Versioning;
using Synentra.Infrastructure.Configuration.Cors;
using Synentra.Infrastructure.Configuration.Persistence;
using Synentra.Mcp;
using Synentra.Persistence.PostgreSql;
using Synentra.Persistence.Sqlite;
using Synentra.Services;
using System.Text.Json.Serialization;

namespace Synentra.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSynentraVersion(this IServiceCollection services)
    {
        services.AddSingleton<IVersion, SynentraVersion>();
        services.AddSingleton<McpSessionRegistry>();
        return services;
    }

    #region Health checks

    public static IServiceCollection AddSynentraHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    #endregion

    #region OpenAPI (Swagger)

    public static IServiceCollection AddSynentraApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("synentra", new OpenApiInfo
            {
                Version = "synentra",
                Title = "Service Invocation",
                Description = "Using the service invocation API to find out how to communicate with Synentra API.",
                License = new OpenApiLicense
                {
                    Name = "Apache License Version 2.0",
                    Url = new Uri("https://www.apache.org/licenses/")
                }
            });
        });

        return services;
    }

    #endregion

    #region JSON options

    public static IServiceCollection AddHttpJsonOptions(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });

        return services;
    }

    #endregion

    #region Persistence

    public static IServiceCollection AddSynentraPersistence(this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var systemConfig = scope.ServiceProvider.GetRequiredService<IOptions<PersistenceConfiguration>>().Value;

        var provider = systemConfig.DefaultProvider;

        switch (provider?.ToLowerInvariant())
        {
            case "sqlite":
                services.AddSynentraSqlite(systemConfig.Providers.Sqlite.ConnectionString);
                break;

            case "postgres":
                services.AddSynentraPostgreSql(systemConfig.Providers.Postgres.ConnectionString);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider '{provider}'.");
        }

        return services;
    }

    #endregion

    #region HttpClient

    public static IServiceCollection AddSynentraProxyForwarder(this IServiceCollection services)
    {
        services.AddHttpClient("ProxyForwarder")
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.ConnectionClose = false;
            });
        return services;
    }

    #endregion

    #region Cors

    public static IServiceCollection AddSynentraCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("SynentraCors", policy =>
            {
                using var scope = services.BuildServiceProvider().CreateScope();
                var corsConfig = scope.ServiceProvider.GetRequiredService<IOptions<CorsConfiguration>>().Value;

                policy.WithOrigins(corsConfig.AllowedOrigins);

                if (corsConfig.AllowedMethods.Contains("*", StringComparer.Ordinal))
                    policy.AllowAnyMethod();
                else
                    policy.WithMethods(corsConfig.AllowedMethods);

                if (corsConfig.AllowedHeaders.Contains("*", StringComparer.Ordinal))
                    policy.AllowAnyHeader();
                else
                    policy.WithHeaders(corsConfig.AllowedHeaders);
            });
        });

        return services;
    }

    #endregion
}