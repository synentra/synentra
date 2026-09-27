namespace Synentra.BuildingBlocks.Configuration.Risk;

public sealed class RiskConfiguration
{
    public bool? Enabled { get; set; } = true;
    public RiskWeightsConfiguration Weights { get; set; } = new();
    public double DefaultUnknownIntentScore { get; set; } = 0.85;
    public string[] DefaultUnknownIntentTags { get; set; } = [];
    public Dictionary<string, IntentRiskProfileConfiguration> IntentProfiles { get; set; } =
        IntentRiskProfileConfiguration.CreateDefaultProfiles();
}
