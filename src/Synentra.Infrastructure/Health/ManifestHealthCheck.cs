using Microsoft.Extensions.Diagnostics.HealthChecks;
using Synentra.Application.Features.Manifests.Contracts;

namespace Synentra.Infrastructure.Health;

public sealed class ManifestHealthCheck(IRevisionProvider revisions, IManifestProvider manifests) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await revisions.ReloadAsync(cancellationToken);
        return manifests.ActiveBundle is null ? HealthCheckResult.Unhealthy("No active governance bundle.") : HealthCheckResult.Healthy();
    }
}
