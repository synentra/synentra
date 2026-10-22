namespace Synentra.Infrastructure.Configuration.BackgroundJobs;

public class ClaimExpirationConfiguration
{
    public bool? Enabled { get; set; } = true;
    public TimeSpan? PollingInterval { get; set; } = TimeSpan.FromSeconds(30);
}
