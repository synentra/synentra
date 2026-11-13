namespace Synentra.Application.Features.Manifests.Models;

public sealed class RiskProfileModel
{
    public string? RiskProfileRef { get; set; }
    public string OnUnavailable { get; set; } = "review";
}
