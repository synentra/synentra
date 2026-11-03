using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Synentra.Application.Common.Security;
using Synentra.Infrastructure.Configuration.OutboundHttp;
using Synentra.Infrastructure.Configuration.Secrets;

namespace Synentra.Infrastructure.Features.Secrets;

public static class BootstrapSecrets
{
    public static SecretsConfiguration Apply(ConfigurationManager configuration)
    {
        var plan = Prepare(configuration, development: false);

        if (plan.Bindings.Values.Any(e =>
                e.Tier == SecretTier.Bootstrap
                && e.Provider is not ("env" or "file")))
        {
            throw new InvalidOperationException(
                "External database bootstrap requires ApplyAsync.");
        }

        ApplyLocal(configuration, plan);
        return plan;
    }

    public static async Task<SecretsConfiguration> ApplyAsync(
        ConfigurationManager configuration,
        bool development,
        CancellationToken ct,
        Func<SecretDeclaration, CancellationToken, Task<ResolvedCredential>>? externalReader = null)
    {
        var plan = Prepare(configuration, development);
        ApplyLocal(configuration, plan);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(plan.Resolution.Timeout);

        var outboundHttpConfiguration =
            configuration.GetSection("OutboundHttp").Get<OutboundHttpConfiguration>() ?? new();

        var values = new Dictionary<string, string?>();

        foreach (var entry in plan.Bindings.Values.Where(e =>
            e.Tier == SecretTier.Bootstrap && e.Provider is not ("env" or "file")))
        {
            try
            {
                var result = externalReader is null
                    ? await ExternalSecretClients.ReadBootstrapAsync(
                        entry, plan, outboundHttpConfiguration, deadline.Token)
                    : await externalReader(entry, deadline.Token);

                SecretPayload.Validate(result);
                values.Add(entry.ConfigurationKey!, result.Value);
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                throw new InvalidOperationException(
                    "External database bootstrap failed.");
            }
        }

        configuration.AddInMemoryCollection(values);
        return plan;
    }

    private static SecretsConfiguration Prepare(
        ConfigurationManager configuration,
        bool development)
    {
        var secrets = configuration.GetSection(SecretsConfiguration.SectionName)
            .Get<SecretsConfiguration>() ?? new();

        var planSection = configuration.GetSection("Secrets");
        var plan = planSection.Get<SecretsConfiguration>() ?? new();

        // Secrets overrides Secrets only for sections that are actually present.
        if (!planSection.GetSection(nameof(SecretsConfiguration.Bindings)).Exists())
            plan.Bindings = secrets.Bindings;

        if (!planSection.GetSection(nameof(SecretsConfiguration.Resolution)).Exists())
            plan.Resolution = secrets.Resolution;

        if (!planSection.GetSection(nameof(SecretsConfiguration.Cache)).Exists())
            plan.Cache = secrets.Cache;

        if (!planSection.GetSection(nameof(SecretsConfiguration.Providers)).Exists())
            plan.Providers = secrets.Providers;

        if (string.IsNullOrWhiteSpace(configuration["Secrets:Provider"]))
            plan.Provider = secrets.Provider;

        // "none" is the unset sentinel.
        if (string.Equals(plan.Provider, "none", StringComparison.OrdinalIgnoreCase))
            plan.Provider = null;

        // Infer a plan-level provider when exactly one remote provider is configured.
        plan.Provider ??= DetectSoleConfiguredProvider(configuration) ?? throw SecretsConfiguration.Invalid();

        foreach (var entry in plan.Bindings.Values)
        {
            if (string.IsNullOrWhiteSpace(entry.Provider))
                entry.Provider = plan.Provider ?? throw SecretsConfiguration.Invalid();
        }

        plan.ValidateDependencies();

        if (!development)
        {
            foreach (var key in SecretsConfiguration.BootstrapKeys)
            {
                if (SecretsConfiguration.MetadataKeys.Contains(key))
                    continue;

                if (string.IsNullOrWhiteSpace(configuration[key]))
                    continue;

                if (SecretsConfiguration.DatabaseKeys.Contains(key)
                    && IsNonsecretConnection(configuration[key]!))
                {
                    continue;
                }

                throw new InvalidOperationException(
                    "Production secret values must use declared references.");
            }
        }

        var database = (configuration["Persistence:Provider"] ?? "sqlite").ToLowerInvariant();
        var connectionKey = database switch
        {
            "sqlite" => "Persistence:Providers:Sqlite:ConnectionString",
            "postgres" or "postgresql" => "Persistence:Providers:Postgres:ConnectionString",
            _ => throw SecretsConfiguration.Invalid()
        };

        var hasConnectionBinding = plan.Bindings.Values.Any(e =>
            e.Tier == SecretTier.Bootstrap
            && e.ConfigurationKey == connectionKey);

        if (!hasConnectionBinding
            && string.IsNullOrWhiteSpace(configuration[connectionKey]))
        {
            throw SecretsConfiguration.Invalid();
        }

        var hasKeyRingBinding = plan.Bindings.Values.Any(e =>
            e.ConfigurationKey == "DataProtection:KeyRingPath");

        if (!hasKeyRingBinding
            && string.IsNullOrWhiteSpace(configuration["DataProtection:KeyRingPath"]))
        {
            throw SecretsConfiguration.Invalid();
        }

        return plan;
    }

    private static string? DetectSoleConfiguredProvider(ConfigurationManager configuration)
    {
        var configured = new List<string>();

        if (configuration["Secrets:Providers:HashiCorpVault:Endpoint"] is not null)
            configured.Add("hashicorpvault");

        if (configuration["Secrets:Providers:AzureKeyVault:Endpoint"] is not null)
            configured.Add("azurekeyvault");

        if (!string.IsNullOrWhiteSpace(configuration["Secrets:Providers:AwsSecrets:Region"]))
            configured.Add("awssecrets");

        return configured.Count == 1 ? configured[0] : null;
    }

    private static bool IsNonsecretConnection(string value)
    {
        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = value };
            return !builder.Keys.Cast<string>().Any(k =>
                (k.Contains("password", StringComparison.OrdinalIgnoreCase)
                 || k.Equals("pwd", StringComparison.OrdinalIgnoreCase)
                 || k.Contains("token", StringComparison.OrdinalIgnoreCase)
                 || k.Contains("secret", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrEmpty(builder[k]?.ToString()));
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyLocal(
        ConfigurationManager configuration,
        SecretsConfiguration plan)
    {
        configuration.AddInMemoryCollection(
            plan.Bindings.Values
                .Where(e => e.Tier == SecretTier.Bootstrap
                            && e.Provider is "env" or "file")
                .ToDictionary(e => e.ConfigurationKey!, e => (string?)Read(e)));
    }

    public static string Read(SecretDeclaration entry)
    {
        try
        {
            string? value;

            if (entry.Provider == "env")
            {
                value = Environment.GetEnvironmentVariable(entry.Path);
            }
            else if (entry.Provider == "file")
            {
                KeyRingSecurity.ValidateSecretFile(entry.Path);
                if (new FileInfo(entry.Path).Length > 65536)
                    throw SecretsConfiguration.Invalid();
                value = File.ReadAllText(entry.Path).TrimEnd('\r', '\n');
            }
            else
            {
                throw SecretsConfiguration.Invalid();
            }

            if (string.IsNullOrWhiteSpace(value)
                || value.Length > 65536
                || value.Any(char.IsControl))
            {
                throw SecretsConfiguration.Invalid();
            }

            return value;
        }
        catch
        {
            throw new InvalidOperationException(
                "Bootstrap secret unavailable or invalid.");
        }
    }
}