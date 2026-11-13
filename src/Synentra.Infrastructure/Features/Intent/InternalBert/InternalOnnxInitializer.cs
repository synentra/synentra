using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synentra.Infrastructure.Configuration.Intent;

namespace Synentra.Infrastructure.Intent.Providers.InternalBert;

internal sealed class InternalOnnxInitializer(
    IServiceProvider serviceProvider,
    IOptions<IntentConfiguration> options,
    ILogger<InternalOnnxInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = options.Value.Providers.Internal;
        if (!config.Enabled)
            return;

        // Resolve the singleton provider (this triggers its constructor registration)
        var onnxProvider = serviceProvider.GetRequiredService<InternalOnnxIntentProvider>();

        // Fire‑and‑forget the actual async initialisation (download + model load)
        _ = Task.Run(async () =>
        {
            try
            {
                await onnxProvider.InitializeAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialise Internal ONNX provider.");
            }
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}