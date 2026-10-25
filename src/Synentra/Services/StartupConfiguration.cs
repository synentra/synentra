using Synentra.Extensions;
using Synentra.Middleware;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Synentra.Infrastructure;
using Synentra.Infrastructure.Features.Secrets;

namespace Synentra.Services;

[ExcludeFromCodeCoverage]
internal sealed class StartupConfiguration : IStartupConfiguration
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // JSON options
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Synentra-specific service registrations
        services
            .AddSynentraPersistence()
            .AddSynentraInfrastructure(configuration)
            .AddSynentraApiDocumentation()
            .AddSynentraProxyForwarder()
            .AddSynentraVersion()
            .AddSynentraCors();

        //builder.AddSynentraObservability();
    }

    public async Task ConfigurePipelineAsync(WebApplication app)
    {
        app.UseSynentraCustomException();

        app.Use(async (context, next) =>
        {
            if (!app.Services.GetRequiredService<StartupSecretState>().IsReady)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { errorCode = "STARTUP_SECRETS_UNAVAILABLE" });
                return;
            }
            await next(context);
        });

        app.UseSynentraCustomHeaders();

        app.UseRouting();
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseSynentraCors();

        app.MapEndpoints();

        app.UseSynentraApiDocumentation();
        app.UseSynentraHealthCheck();

        // Ensure database is created/migrated
        await app.EnsureApplicationDatabaseCreated();
    }
}