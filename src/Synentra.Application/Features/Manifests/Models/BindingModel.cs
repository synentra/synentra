namespace Synentra.Application.Features.Manifests.Models;

public sealed class BindingModel
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "1";
    public string OperationId { get; set; } = "";
    public string TargetRef { get; set; } = "";
    public Dictionary<string, object> Configuration { get; set; } = new();
    public Dictionary<string, MappingEntryModel>? CanonicalToTarget { get; set; }
    public Dictionary<string, MappingEntryModel>? TargetToCanonical { get; set; }
}
