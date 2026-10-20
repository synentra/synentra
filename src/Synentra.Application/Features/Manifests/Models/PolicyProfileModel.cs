namespace Synentra.Application.Features.Manifests.Models;

public sealed class PolicyProfileModel
{
    public string CombiningAlgorithm { get; set; } = "deny_overrides";
    public string DefaultOutcome { get; set; } = "deny";
}
