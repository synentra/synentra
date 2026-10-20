using System.Text.Json;

namespace Synentra.Application.Features.Manifests.Models;

public sealed class PolicyMatchModel
{
    public List<string>? AgentIds { get; set; }
    public List<string>? OperationIds { get; set; }
    public List<string>? OperationFamilies { get; set; }
    public List<ResourceMatchModel>? ResourceMatches { get; set; }
    public Dictionary<string, JsonElement>? ArgumentEquals { get; set; }
    public Dictionary<string, long>? ArgumentIntegerGreaterThan { get; set; }
}
