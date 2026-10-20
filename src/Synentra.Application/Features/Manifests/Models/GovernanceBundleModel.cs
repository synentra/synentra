namespace Synentra.Application.Features.Manifests.Models;

public sealed class GovernanceBundleModel
{
    public string ApiVersion { get; set; } = "";
    public string Kind { get; set; } = "";
    public BundleMetadata Metadata { get; set; } = new();
    public BundleSpec Spec { get; set; } = new();
}
