using Microsoft.AspNetCore.Mvc;
using Synentra.Diagnostics.Catalog;
using Synentra.Extensions;
using Synentra.Infrastructure;
using Synentra.Infrastructure.Features.Secrets;
using Synentra.Middleware;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

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
            .AddSynentraInfrastructure(configuration)
            .AddSynentraPersistence()
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
                string GetCorrelationId(HttpContext context)
                {
                    if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var value)
                        && Guid.TryParse(value.ToString(), out var supplied))
                        return supplied.ToString("D");

                    return Guid.NewGuid().ToString("D");
                }

                var diagnostic = InfrastructureDiagnostics.StartupSecretsUnavailable;
                var status = StatusCodes.Status503ServiceUnavailable;
                var correlationId = GetCorrelationId(context);

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";
                var problem = new ProblemDetails {
                    Title = diagnostic.Message,
                    Status = status,
                    Type = $"https://httpstatuses.com/{status}"
                };
                problem.Extensions["errorCode"] = diagnostic.Code;
                problem.Extensions["traceId"] = context.TraceIdentifier;
                problem.Extensions["correlationId"] = correlationId;
                context.Response.Headers["X-Correlation-Id"] = correlationId;

                await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
                return;
            }
            await next(context);
        });

        app.UseMiddleware<IdempotencyMiddleware>();

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