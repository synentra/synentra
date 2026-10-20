namespace Synentra.Application.Features.Manifests.Models;

public sealed class ProfileModel
{
    public string Id { get; set; } = "";
    public IntentProfileModel? Intent { get; set; }
    public TrustProfileModel? Trust { get; set; }
    public RiskProfileModel? Risk { get; set; }
    public PolicyProfileModel? Policy { get; set; }
}
