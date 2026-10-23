namespace Synentra.Infrastructure.Configuration.BackgroundJobs;

public class BackgroundJobsConfiguration: ISynentraConfiguration
{
    public static string SectionName => "BackgroundJobs";

    public OutboxConfiguration Outbox { get; set; } = new();
    public ClaimExpirationConfiguration ClaimExpiration { get; set; } = new();
    public InitializationConfiguration Initialization { get; set; } = new();
}
