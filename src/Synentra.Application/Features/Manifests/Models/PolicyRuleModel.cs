namespace Synentra.Application.Features.Manifests.Models;

public sealed class PolicyRuleModel
{
    public string Id { get; set; } = "";
    public PolicyMatchModel Match { get; set; } = new();
    public string Effect { get; set; } = "deny";
}
