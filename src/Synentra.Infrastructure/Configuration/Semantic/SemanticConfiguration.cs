namespace Synentra.Infrastructure.Configuration.Semantic;

public class SemanticConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Semantic";

    public SemanticProviders Providers { get; set; } = new();
}