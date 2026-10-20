namespace Synentra.Application.Features.Manifests.Models;

public sealed class RestrictionModel
{
    public string Id { get; set; } = "";
    public RestrictionMatchModel Match { get; set; } = new();
    public string Effect { get; set; } = "deny";
}
