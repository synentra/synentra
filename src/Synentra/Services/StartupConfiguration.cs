using Microsoft.AspNetCore.DataProtection;
using Synentra.Extensions;
using Synentra.Middleware;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Synentra.Infrastructure;

namespace Synentra.Services;

[ExcludeFromCodeCoverage]
internal sealed class StartupConfiguration : IStartupConfiguration
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        //// Data Protection
        //var dpKeysDir = Path.Combine(
        //    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        //    "Synentra",
        //    "DataProtection",
        //    "Keys");

        //Directory.CreateDirectory(dpKeysDir);

        //var dataProtectionBuilder = services.AddDataProtection()
        //    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysDir))
        //    .SetApplicationName("Synentra");

        //// Apply DPAPI encryption only on Windows
        //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //    dataProtectionBuilder.ProtectKeysWithDpapi();

        // JSON options
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Synentra-specific service registrations
        services
            .AddSynentraConfiguration(configuration)
            .AddSynentraPersistence()
            .AddSynentraInfrastructure(configuration)
            .AddSynentraApiDocumentation()
            .AddSynentraProxyForwarder()
            .AddSynentraVersion()
            .AddSynentraCors();

        //builder.AddSynentraObservability();

        // HTTP server configuration (Kestrel, etc.)
        builder.ConfigureSynentraHttpServer();
    }

    public async Task ConfigurePipelineAsync(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseSynentraCustomException();
        }

        app.UseSynentraHttps();
        app.UseSynentraCustomHeaders();

        app.UseRouting();
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseSynentraCors();

        app.MapEndpoints();

        app.UseSynentraApiDocumentation();
        //app.UseSynentraHealthCheck();

        // Ensure database is created/migrated
        await app.EnsureApplicationDatabaseCreated();
    }
}