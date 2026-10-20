namespace Synentra.Application.Features.Manifests.Models;

public sealed class RestrictionMatchModel
{
    public List<string>? AgentIds { get; set; }
    public List<string>? OperationFamilies { get; set; }
    public List<string>? OperationIds { get; set; }
}
