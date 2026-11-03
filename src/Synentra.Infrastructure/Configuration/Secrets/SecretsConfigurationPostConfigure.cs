using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Secrets;

public sealed class SecretsConfigurationPostConfigure
    : IPostConfigureOptions<SecretsConfiguration>
{
    public void PostConfigure(string? name, SecretsConfiguration options)
    {
        options.Resolution ??= new SecretResolutionConfiguration();
        options.Cache ??= new SecretCacheOptions();
        options.Bindings ??= new Dictionary<string, SecretDeclaration>(StringComparer.Ordinal);
    }
}