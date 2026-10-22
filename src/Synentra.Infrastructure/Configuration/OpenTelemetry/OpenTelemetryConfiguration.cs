namespace Synentra.Infrastructure.Configuration.OpenTelemetry;

public class OpenTelemetryConfiguration
{
    public bool Enabled { get; set; } = false;
    public string? Endpoint { get; set; }
    public string? ServiceName { get; set; } = "synentra";
    public bool? ExportLogs { get; set; } = false;
    public bool? ExportTraces { get; set; } = true;
    public bool? ExportMetrics { get; set; } = true;
}
