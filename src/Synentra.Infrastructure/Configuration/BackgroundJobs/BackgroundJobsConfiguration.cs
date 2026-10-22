namespace Synentra.Infrastructure.Configuration.BackgroundJobs;

public class BackgroundJobsConfiguration
{
    public OutboxConfiguration Outbox { get; set; } = new();

}
