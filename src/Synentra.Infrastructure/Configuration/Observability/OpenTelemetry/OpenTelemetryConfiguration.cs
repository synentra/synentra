namespace Synentra.Infrastructure.Configuration.Observability.OpenTelemetry;

public class OpenTelemetryConfiguration
{
    public bool Enabled { get; set; } = false;
    public string? Endpoint { get; set; }
}
