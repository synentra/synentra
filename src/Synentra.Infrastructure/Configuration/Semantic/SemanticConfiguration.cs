namespace Synentra.Infrastructure.Configuration.Semantic;

public class SemanticConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Semantic";

    public bool Enabled { get; set; } = true;
    public double ConfidenceThreshold { get; set; } = 0.7;
    public bool AllowLowConfidence { get; set; } = false;
    public string DefaultProvider { get; set; } = "Internal";
    public SemanticProviders Providers { get; set; } = new();
}