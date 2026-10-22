namespace Synentra.Infrastructure.Configuration.BackgroundJobs;

public class OutboxConfiguration
{
    public bool? Enabled { get; set; } = true;
    public TimeSpan? PollingInterval { get; set; } = TimeSpan.FromSeconds(2);
    public int? MaxAttempts { get; set; } = 5;
}
