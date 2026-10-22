namespace Synentra.Infrastructure.Configuration.Trust;

public class TrustConfiguration
{
    public int? InitialScore { get; set; } = 50;
    public int? QuarantineThreshold { get; set; } = 10;
    public Dictionary<string, double> EvidenceWeights { get; set; } = new();
}