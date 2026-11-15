namespace Synentra.Application.Features.Manifests.Models;

public sealed class ResponseRetentionModel
{
    public string Mode { get; set; } = "json-allowlist";
    public string[] AllowedProperties { get; set; } = [];

    public void Validate()
    {
        if (Mode is not ("omit" or "json-allowlist")
            || AllowedProperties.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Invalid authenticated response retention policy.");
    }
}
