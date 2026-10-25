namespace Synentra.Infrastructure.Configuration.DataProtection;

public sealed class AuthenticatedResponsePolicy
{
    public string Mode { get; set; } = "omit";
    public string[] AllowedProperties { get; set; } = [];
    public void Validate()
    {
        if (Mode is not ("omit" or "json-allowlist")
            || (Mode == "json-allowlist" && AllowedProperties.Length == 0)
            || AllowedProperties.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Invalid authenticated response retention policy.");
    }
}