namespace Synentra.Application.Features.Manifests.Models;

public sealed class TargetModel
{
    public string Id { get; set; } = "";
    public string AdapterRef { get; set; } = "";
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string? CredentialBindingRef { get; set; }
    public string? CapabilityProfileRef { get; set; }
}
