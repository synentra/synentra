using Synentra.Infrastructure.Configuration.Observability.Logging;
using Synentra.Infrastructure.Configuration.Observability.OpenTelemetry;

namespace Synentra.Infrastructure.Configuration.Observability;

public class ObservabilityConfiguration
{
    public LoggingConfiguration Logging { get; set; } = new();
    public OpenTelemetryConfiguration? OpenTelemetry { get; set; } = new();
}
