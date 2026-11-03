using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class StartupSecretHealthCheck(StartupSecretState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
        => Task.FromResult(state.IsReady 
            ? HealthCheckResult.Healthy() 
            : HealthCheckResult.Unhealthy("Mandatory startup credentials unavailable or expired."));
}