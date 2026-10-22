using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Synentra.Application.Common.Versioning;
using Synentra.Infrastructure.Configuration.Approvals;
using Synentra.Infrastructure.Configuration.BackgroundJobs;
using Synentra.Infrastructure.Configuration.Cors;
using Synentra.Infrastructure.Configuration.DataProtection;
using Synentra.Infrastructure.Configuration.Logging;
using Synentra.Infrastructure.Configuration.OpenTelemetry;
using Synentra.Infrastructure.Configuration.OutboundHttp;
using Synentra.Infrastructure.Configuration.Persistence;
using Synentra.Infrastructure.Configuration.Policy;
using Synentra.Infrastructure.Configuration.SecretManagement;
using Synentra.Infrastructure.Configuration.Security;
using Synentra.Infrastructure.Configuration.Semantic;
using Synentra.Infrastructure.Configuration.Trust;
using Synentra.Mcp;
using Synentra.Persistence.PostgreSql;
using Synentra.Persistence.Sqlite;
using Synentra.Services;
using System.Text.Json.Serialization;

namespace Synentra.Extensions;

public static class ServiceCollectionExtensions
{
    //private const string ApprovalsConfigurationName = "Approvals";
    //private const string BackgroundJobsConfigurationName = "BackgroundJobs";
    //private const string CorsConfigurationName = "Cors";
    //private const string DataProtectionConfigurationName = "DataProtection";
    //private const string IdentityConfigurationName = "Identity";
    //private const string LoggingConfigurationName = "Logging";
    //private const string OpenTelemetryConfigurationName = "OpenTelemetry";
    //private const string OutboundHttpConfigurationName = "OutboundHttp";
    //private const string PersistenceConfigurationName = "Persistence";
    //private const string PolicyConfigurationName = "Policy";
    //private const string SecretManagementConfigurationName = "SecretManagement";
    //private const string SemanticConfigurationName = "Semantic";
    //private const string TrustConfigurationName = "Trust";

    //#region Simple registrations

    public static IServiceCollection AddSynentraVersion(this IServiceCollection services)
    {
        services.AddSingleton<IVersion, SynentraVersion>();
        services.AddSingleton<McpSessionRegistry>();
        return services;
    }

    //public static IServiceCollection AddSynentraConfiguration(this IServiceCollection services, IConfiguration configuration)
    //{
    //    services.Configure<ApprovalsConfiguration>(configuration.GetSection(ApprovalsConfigurationName));
    //    services.Configure<BackgroundJobsConfiguration>(configuration.GetSection(BackgroundJobsConfigurationName));
    //    services.Configure<CorsConfiguration>(configuration.GetSection(CorsConfigurationName));
    //    services.Configure<DataProtectionConfiguration>(configuration.GetSection(DataProtectionConfigurationName));
    //    services.Configure<IdentityConfiguration>(configuration.GetSection(IdentityConfigurationName));
    //    services.Configure<LoggingConfiguration>(configuration.GetSection(LoggingConfigurationName));
    //    services.Configure<OpenTelemetryConfiguration>(configuration.GetSection(OpenTelemetryConfigurationName));
    //    services.Configure<OutboundHttpConfiguration>(configuration.GetSection(OutboundHttpConfigurationName));
    //    services.Configure<PersistenceConfiguration>(configuration.GetSection(PersistenceConfigurationName));
    //    services.Configure<PolicyConfiguration>(configuration.GetSection(PolicyConfigurationName));
    //    services.Configure<SecretManagementConfiguration>(configuration.GetSection(SecretManagementConfigurationName));
    //    services.Configure<SemanticConfiguration>(configuration.GetSection(SemanticConfigurationName));
    //    services.Configure<TrustConfiguration>(configuration.GetSection(TrustConfigurationName));

    //    return services;
    //}

    //#endregion

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