using Microsoft.Extensions.Options;
using Synentra.Extensions;

namespace Synentra.Services;

internal sealed class SynentraApplicationRunner : ISynentraApplicationRunner
{
    private readonly IWebApplicationFactory _factory;
    private readonly ISplashScreen _splashScreen;
    private readonly IStartupConfiguration _startupConfiguration;

    public SynentraApplicationRunner(
            IWebApplicationFactory factory,
            ISplashScreen splashScreen,
            IStartupConfiguration startupConfiguration)
    {
        _factory = factory;
        _splashScreen = splashScreen;
        _startupConfiguration = startupConfiguration;
    }

    public async Task RunAsync(
        string[] args,
        CancellationToken cancellationToken)
    {
        _splashScreen.Render();

        var builder = _factory.Create(args);

        try
        {
            await builder.AddSynentraSecretManagementAsync(cancellationToken);

            builder.Logging.AddFilter(
                "Microsoft.AspNetCore.Hosting.Diagnostics",
                LogLevel.Warning);

            _startupConfiguration.ConfigureServices(builder);

            await using var app = builder.Build();

            await _startupConfiguration.ConfigurePipelineAsync(app);

            //await app.InitializeStartupSecretsAsync(builder.Configuration, cancellationToken);

            await app.RunAsync(cancellationToken);
        }
        catch (OptionsValidationException ex)
        {
            await HandleValidationFailureAsync(builder, ex);
        }
        catch (Exception ex)
        {
            await HandleStartupFailureAsync(builder, ex);
        }
    }

    internal static Action<int> ExitAction { get; set; } = Environment.Exit;

    internal static async Task HandleValidationFailureAsync(
        WebApplicationBuilder builder,
        OptionsValidationException ex)
    {
        try
        {
            using var scope =
                builder.Services.BuildServiceProvider().CreateScope();

            var logger =
                scope.ServiceProvider.GetService<ILogger<Program>>();

            foreach (var error in ex.Failures)
            {
                logger?.LogError($"Configuration error: {error}");
            }
        }
        catch
        {
            foreach (var error in ex.Failures)
            {
                await Console.Error.WriteLineAsync($"Configuration error: {error}");
            }
        }

        await Task.Delay(500);

        ExitAction(1);
    }

    internal static async Task HandleStartupFailureAsync(
        WebApplicationBuilder builder,
        Exception ex)
    {
        try
        {
            using var scope =
                builder.Services.BuildServiceProvider().CreateScope();

            var logger =
                scope.ServiceProvider.GetService<ILogger<Program>>();

            logger?.LogCritical(
                ex,
                "Unhandled exception during application startup");
        }
        catch
        {
            await Console.Error.WriteLineAsync(
                $"Startup error: {ex.Message}");
        }

        await Task.Delay(500);

        ExitAction(1);
    }
}