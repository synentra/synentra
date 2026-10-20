namespace Synentra.Application.Features.Manifests.Models;

public sealed class VerificationModel
{
    public string VerifierRef { get; set; } = "";
    public string Version { get; set; } = "1";
    public Dictionary<string, object> Configuration { get; set; } = new();
}
