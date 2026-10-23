namespace Synentra.Infrastructure.Configuration.Trust;

public class TrustConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Trust";

    public int? InitialScore { get; set; } = 50;
    public int? QuarantineThreshold { get; set; } = 10;
    public Dictionary<string, double>? EvidenceWeights { get; set; } = new();
}