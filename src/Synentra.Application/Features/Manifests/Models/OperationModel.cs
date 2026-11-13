using System.Text.Json;

namespace Synentra.Application.Features.Manifests.Models;

public sealed class OperationModel
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "1";
    public string Family { get; set; } = "";
    public string Action { get; set; } = "";
    public string Mutability { get; set; } = "readOnly";
    public ExecutionMode Execution { get; set; } = new();
    public string? GovernanceProfileRef { get; set; }
    public List<string> PolicyRefs { get; set; } = new();
    public bool RequiresTaskGrant { get; set; }
    public JsonElement InputSchema { get; set; }
    public List<ResolverModel> Resources { get; set; } = new();
    public VerificationModel? Verification { get; set; }
}
