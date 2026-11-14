namespace Synentra.Application.Features.Manifests.Models;

public sealed class TrustProfileModel
{
    public string? ProviderRef { get; set; }
    public string OnUnavailable { get; set; } = "deny";
}
