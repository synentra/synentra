using Microsoft.Extensions.Hosting;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class StartupSecretGate(StartupSecretState state) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!state.IsReady)
            throw new InvalidOperationException("Mandatory secret initialization has not completed.");

        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}