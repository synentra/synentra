using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Secrets;

public sealed class SecretsConfigurationValidator
    : IValidateOptions<SecretsConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        SecretsConfiguration options)
    {
        if (name is not null && name != Options.DefaultName)
            return ValidateOptionsResult.Skip;

        if (options is null)
            return ValidateOptionsResult.Fail("Secrets configuration is required.");

        var failures = new List<string>();
        var providers = options.Providers;

        // Provider objects are often initialized with new(), so the section
        // may be present but effectively empty. Check bound values, not object
        // identity.
        var hasAws = !string.IsNullOrWhiteSpace(providers?.AwsSecrets?.Region);
        var hasAzure = providers?.AzureKeyVault?.Endpoint is not null;
        var hasVault = providers?.HashiCorpVault?.Endpoint is not null;

        var isNoSecretsMode =
            string.Equals(options.Provider, "none", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(options.Provider)
                && options.Bindings is not null
                && options.Bindings.Count == 0
                && !hasAws
                && !hasAzure
                && !hasVault);

        if (!isNoSecretsMode && !hasAws && !hasAzure && !hasVault)
        {
            failures.Add(
                "Secrets:Providers must configure at least one of AwsSecrets, AzureKeyVault, or HashiCorpVault.");
        }

        if (options.Bindings is not null)
        {
            foreach (var (bindingName, declaration) in options.Bindings)
            {
                if (string.IsNullOrWhiteSpace(bindingName))
                    failures.Add("Secrets:Bindings contains an empty binding name.");

                if (declaration is null)
                {
                    failures.Add($"Secrets:Bindings:{bindingName} is required.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(declaration.Provider))
                    failures.Add($"Secrets:Bindings:{bindingName}:Provider is required.");

                if (declaration.DependsOn is null)
                {
                    failures.Add($"Secrets:Bindings:{bindingName}:DependsOn cannot be null.");
                }
                else
                {
                    foreach (var dependency in declaration.DependsOn)
                    {
                        if (string.IsNullOrWhiteSpace(dependency))
                        {
                            failures.Add(
                                $"Secrets:Bindings:{bindingName}:DependsOn contains an empty dependency.");
                        }
                    }
                }
            }
        }

        if (failures.Count > 0)
            return ValidateOptionsResult.Fail(failures);

        try
        {
            options.ValidateDependencies();

            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException)
        {
            return ValidateOptionsResult.Fail(
                "Secrets configuration contains an invalid secret plan, " +
                "provider configuration, tier dependency, or dependency cycle.");
        }
    }
}