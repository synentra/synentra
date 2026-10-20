namespace Synentra.Application.Features.Manifests.Models;

public sealed class ResolverModel
{
    public string Role { get; set; } = "";
    public string ResolverRef { get; set; } = "";
    public string Version { get; set; } = "1";
    public Dictionary<string, object> Configuration { get; set; } = new();
}
