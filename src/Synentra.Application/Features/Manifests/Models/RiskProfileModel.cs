namespace Synentra.Application.Features.Manifests.Models;

public sealed class RiskProfileModel
{
    public string? ProfileRef { get; set; }
    public string OnUnavailable { get; set; } = "review";
}
