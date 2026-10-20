namespace Synentra.Application.Features.Manifests.Models;

public sealed class IntentProfileModel
{
    public string? ProviderRef { get; set; }
    public bool Required { get; set; }
    public double MinimumConfidence { get; set; } = 0.8;
    public string OnUnavailable { get; set; } = "review";
    public string OnLowConfidence { get; set; } = "review";
}
