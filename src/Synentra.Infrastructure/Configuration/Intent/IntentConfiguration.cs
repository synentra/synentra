namespace Synentra.Infrastructure.Configuration.Intent;

public class IntentConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Intent";

    public IntentProviders Providers { get; set; } = new();
}