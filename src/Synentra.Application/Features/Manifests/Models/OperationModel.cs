using System.Text.Json;

namespace Synentra.Application.Features.Manifests.Models;

public sealed class OperationModel
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "1";
    public string Family { get; set; } = "";
    public string Action { get; set; } = "";
    public string Mutability { get; set; } = "read_only";
    public ExecutionModelModel Execution { get; set; } = new();
    public string? ProfileRef { get; set; }
    public List<string> PolicyRefs { get; set; } = new();
    public bool TaskGrantRequired { get; set; }
    public JsonElement InputSchema { get; set; }
    public List<ResolverModel> Resources { get; set; } = new();
    public EffectResolverModel? Effect { get; set; }
    public VerificationModel? Verification { get; set; }
}
