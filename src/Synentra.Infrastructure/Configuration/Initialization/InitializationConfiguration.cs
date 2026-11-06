namespace Synentra.Infrastructure.Configuration.Initialization;

public class InitializationConfiguration : ISynentraConfiguration
{
    public static string SectionName => "Initialization";

    public bool? Enabled { get; set; } = true;
    public TimeSpan? PollingInterval { get; set; } = TimeSpan.FromSeconds(30);
    public ManifestBootstrapConfiguration ManifestBootstrap { get; set; } = new();
}