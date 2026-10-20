namespace Synentra.Application.Features.Manifests.Models;

public sealed class PolicyRuleModel
{
    public PolicyMatchModel Match { get; set; } = new();
    public string Effect { get; set; } = "deny";
}
