namespace Synentra.Application.Features.Manifests.Models;

public sealed class ResourceMatchModel
{
    public string Role { get; set; } = "";
    public string Type { get; set; } = "";
    public List<string>? Ids { get; set; }
    public string? Quantifier { get; set; }
}
