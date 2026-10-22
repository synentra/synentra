namespace Synentra.Infrastructure.Configuration.BackgroundJobs;

public class InitializationConfiguration
{
    public bool? Enabled { get; set; } = true;
    public TimeSpan? PollingInterval { get; set; } = TimeSpan.FromSeconds(30);
    public ManifestBootstrapConfiguration ManifestBootstrap { get; set; } = new();
}

public class ManifestBootstrapConfiguration
{
    public bool? Enabled { get; set; } = true;
}