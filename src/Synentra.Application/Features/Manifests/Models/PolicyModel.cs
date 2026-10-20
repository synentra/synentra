namespace Synentra.Application.Features.Manifests.Models;

public sealed class PolicyModel
{
    public string Id { get; set; } = "";
    public string? OperationFamily { get; set; }
    public List<PolicyRuleModel> Rules { get; set; } = new();
}
