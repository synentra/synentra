namespace Synentra.Infrastructure.Configuration.Policy;

public class PolicyConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Policy";

    public string Provider { get; set; } = "Declarative";
    public PolicyProviders Providers { get; set; } = new();
}