using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using Synentra.Middleware;
using Synentra.Persistence;
using Synentra.Infrastructure.Configuration.Cors;

namespace Synentra.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSynentraCustomException(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseSynentraCustomHeaders(this IApplicationBuilder app)
    {
        // Inject IVersion via middleware instead of locating from ApplicationServices
        app.UseMiddleware<VersionHeaderMiddleware>();
        return app;
    }

    [ExcludeFromCodeCoverage]
    public static IApplicationBuilder UseSynentraApiDocumentation(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        app.UseSwagger(options =>
        {
            options.RouteTemplate = $"open-api/{{documentName}}/specifications.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "open-api";
            options.SwaggerEndpoint($"synentra/specifications.json", $"Synentra API");
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapSwagger();
        });

        return app;
    }

    public static async Task<IApplicationBuilder> EnsureApplicationDatabaseCreated(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        var initializers = serviceScope.ServiceProvider.GetServices<IDatabaseInitializer>();

        foreach (var initializer in initializers)
        {
            await initializer.EnsureDatabaseCreatedAsync();
        }

        return app;
    }

    public static IApplicationBuilder UseSynentraCors(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var serverConfiguration = scope.ServiceProvider.GetRequiredService<IOptions<CorsConfiguration>>();
        var allowedOrigins = serverConfiguration.Value.AllowedOrigins;
        if (allowedOrigins is not null && allowedOrigins.Length != 0)
            app.UseCors("SynentraCors");

        return app;
    }
}