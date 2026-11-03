using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Infrastructure.Configuration.Secrets;

public sealed class SecretDeclaration
{
    public SecretTier Tier { get; set; }
    public string Provider { get; set; } = "";
    public string Path { get; set; } = "";
    public string Version { get; set; } = "";
    public string? ConfigurationKey { get; set; }
    public string[] DependsOn { get; set; } = [];
    public override string ToString() => "SecretDeclaration([REDACTED])";
    public SecretProviderRef Reference => new(Provider, Path, Version);
}