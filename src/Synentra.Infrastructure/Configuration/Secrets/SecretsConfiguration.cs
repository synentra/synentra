using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Features.Secrets;

namespace Synentra.Infrastructure.Configuration.Secrets;

public class SecretsConfiguration : ISynentraConfiguration
{
    public static string SectionName => "Secrets";

    public string Provider { get; set; } = "none";
    public SecretProviders Providers { get; set; } = new();
    public SecretResolutionConfiguration Resolution { get; set; } = new();
    public SecretCacheOptions Cache { get; set; } = new();
    public Dictionary<string, SecretDeclaration> Bindings { get; set; } = new(StringComparer.Ordinal);

    public static readonly HashSet<string> RemoteProviders = new(StringComparer.Ordinal)
    {
        "localvault",
        "hashicorpvault",
        "azurekeyvault",
        "awssecrets"
    };

    public static readonly HashSet<string> DatabaseKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Persistence:Providers:Sqlite:ConnectionString",
        "Persistence:Providers:Postgres:ConnectionString"
    };

    public static readonly HashSet<string> MetadataKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "DataProtection:KeyRingPath",
        "DataProtection:Certificate:Path"
    };

    // These keys must match the bound property names on the provider classes.
    public static readonly HashSet<string> BootstrapKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Persistence:Providers:Sqlite:ConnectionString",
        "Persistence:Providers:Postgres:ConnectionString",
        "DataProtection:KeyRingPath",
        "DataProtection:Certificate:Path",
        "DataProtection:Certificate:Password",
        "Secrets:Providers:AwsSecrets:AccessKey",
        "Secrets:Providers:AwsSecrets:SecretKey",
        "Secrets:Providers:AwsSecrets:SessionToken",
        "Secrets:Providers:AzureKeyVault:ClientSecret",
        "Secrets:Providers:HashiCorpVault:Token"
    };

    public static readonly HashSet<string> StartupKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Identity:Ldap:ServicePassword",
        "Policy:Providers:Opa:OpaBearerToken"
    };

    public static bool IsStartupKey(string key) => StartupKeys.Contains(key);

    /// <summary>
    /// Structural validation only: the plan is internally well-formed.
    /// Provider availability is checked by <see cref="ValidateDependencies"/>.
    /// </summary>
    public void Validate()
    {
        if (Resolution is null || Cache is null || Bindings is null)
            throw Invalid();

        if (Resolution.Timeout < TimeSpan.FromSeconds(1)
            || Resolution.Timeout > TimeSpan.FromSeconds(300))
        {
            throw Invalid();
        }

        Cache.Validate();

        var bindingsByName = Bindings.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);

        foreach (var (name, entry) in bindingsByName)
        {
            if (string.IsNullOrWhiteSpace(name)
                || entry is null
                || !Enum.IsDefined(entry.Tier)
                || string.IsNullOrWhiteSpace(entry.Provider)
                || string.IsNullOrWhiteSpace(entry.Path)
                || entry.DependsOn is null)
            {
                throw Invalid();
            }

            var local = entry.Provider is "env" or "file";

            var databaseBootstrap = entry.Tier == SecretTier.Bootstrap
                && entry.ConfigurationKey is { } destination
                && DatabaseKeys.Contains(destination)
                && entry.Provider is "hashicorpvault" or "azurekeyvault" or "awssecrets";

            var tierProviderValid = entry.Tier switch
            {
                SecretTier.Bootstrap => local || databaseBootstrap,
                SecretTier.Startup => local || RemoteProviders.Contains(entry.Provider),
                SecretTier.Runtime => RemoteProviders.Contains(entry.Provider),
                _ => false
            };

            if (!tierProviderValid)
                throw Invalid();

            if (!local && string.IsNullOrWhiteSpace(entry.Version))
                throw Invalid();

            if (entry.Tier == SecretTier.Runtime)
            {
                if (entry.ConfigurationKey is not null)
                    throw Invalid();
            }
            else if (entry.ConfigurationKey is null
                     || !(entry.Tier == SecretTier.Bootstrap
                         ? BootstrapKeys.Contains(entry.ConfigurationKey)
                         : IsStartupKey(entry.ConfigurationKey)))
            {
                throw Invalid();
            }

            foreach (var dependency in entry.DependsOn)
            {
                if (string.IsNullOrWhiteSpace(dependency)
                    || !bindingsByName.TryGetValue(dependency, out var parent))
                {
                    throw Invalid();
                }

                var permittedBootstrapDependency =
                    databaseBootstrap
                    && parent.Tier == SecretTier.Bootstrap
                    && parent.Provider is "env" or "file";

                if (parent.Tier >= entry.Tier && !permittedBootstrapDependency)
                    throw Invalid();
            }
        }

        var destinations = bindingsByName.Values
            .Where(e => e.ConfigurationKey is not null)
            .Select(e => e.ConfigurationKey!)
            .ToArray();

        if (destinations.Distinct(StringComparer.OrdinalIgnoreCase).Count() != destinations.Length)
            throw Invalid();
    }

    /// <summary>
    /// Structural + intent validation: applies defaults, requires at least one
    /// configured provider, calls <see cref="Validate"/>, then builds the secret
    /// dependency graph and rejects cycles.
    /// </summary>
    public void ValidateDependencies()
    {
        Resolution ??= new SecretResolutionConfiguration();
        Cache ??= new SecretCacheOptions();
        Bindings ??= new Dictionary<string, SecretDeclaration>(StringComparer.Ordinal);

        var providers = Providers ?? new SecretProviders();

        var vault = providers.HashiCorpVault;
        var azure = providers.AzureKeyVault;
        var aws = providers.AwsSecrets;

        var hasVault = vault?.Endpoint is not null && !string.IsNullOrWhiteSpace(vault?.Endpoint);
        var hasAzure = azure?.Endpoint is not null && !string.IsNullOrWhiteSpace(azure?.Endpoint);
        var hasAws = !string.IsNullOrWhiteSpace(aws?.Region);

        var isNoSecretsMode =
            string.Equals(Provider, "none", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(Provider)
                && Bindings.Count == 0
                && !hasVault
                && !hasAzure
                && !hasAws);

        if (isNoSecretsMode)
            return;

        if (!hasVault && !hasAzure && !hasAws)
        {
            throw new InvalidOperationException(
                "Secrets:Providers must configure at least one of AwsSecrets, AzureKeyVault, or HashiCorpVault.");
        }

        Validate();

        if (Bindings is null)
            Bindings = new Dictionary<string, SecretDeclaration>(StringComparer.Ordinal);

        var bindingCount = Bindings.Count;
        if (bindingCount == 0)
            return;

        var bindingsByName = Bindings.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);

        var destinations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, entry) in bindingsByName)
        {
            if (entry.ConfigurationKey is { } key && !destinations.TryAdd(key, name))
            {
                throw new InvalidOperationException(
                    "Multiple secret bindings target the same configuration key.");
            }
        }

        var graph = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        void AddNode(string node)
        {
            if (!graph.ContainsKey(node))
                graph.Add(node, new HashSet<string>(StringComparer.Ordinal));
        }

        void AddEdge(string node, string dependency)
        {
            AddNode(node);
            AddNode(dependency);
            graph[node].Add(dependency);
        }

        static string SecretNode(string name) => "secret:" + name;
        static string ProviderNode(string provider) => "provider:" + provider;

        void AddCredentialRoot(string provider, string configurationKey)
        {
            if (!destinations.TryGetValue(configurationKey, out var name))
            {
                throw new InvalidOperationException(
                    $"Provider '{provider}' requires a bootstrap binding for '{configurationKey}'.");
            }

            var entry = bindingsByName[name];

            if (entry.Tier != SecretTier.Bootstrap
                || entry.Provider is not ("env" or "file"))
            {
                throw new InvalidOperationException(
                    $"Provider '{provider}' credentials must come from bootstrap env or file bindings.");
            }

            AddEdge(ProviderNode(provider), SecretNode(name));
        }

        if (hasVault)
        {
            ExternalSecretClients.ValidateEndpoint(vault!.Endpoint);

            AddCredentialRoot(
                "hashicorpvault",
                "Secrets:Providers:HashiCorpVault:Token");
        }

        if (hasAzure)
        {
            ExternalSecretClients.ValidateEndpoint(azure!.Endpoint);

            switch (azure.Identity)
            {
                case "client-secret":
                    AddCredentialRoot(
                        "azurekeyvault",
                        "Secrets:Providers:AzureKeyVault:ClientSecret");
                    break;

                case "workload":
                case "managed":
                case "managed-user":
                    break;

                default:
                    throw new InvalidOperationException(
                        "Secrets:Providers:AzureKeyVault:Identity must be client-secret, workload, managed, or managed-user.");
            }
        }

        if (hasAws)
        {
            switch (aws!.Identity)
            {
                case "bootstrap":
                    AddCredentialRoot(
                        "awssecrets",
                        "Secrets:Providers:AwsSecrets:AccessKey");

                    AddCredentialRoot(
                        "awssecrets",
                        "Secrets:Providers:AwsSecrets:SecretKey");

                    const string sessionTokenKey =
                        "Secrets:Providers:AwsSecrets:SessionToken";

                    if (destinations.ContainsKey(sessionTokenKey))
                        AddCredentialRoot("awssecrets", sessionTokenKey);

                    break;

                case "web-identity":
                case "instance-role":
                    break;

                default:
                    throw new InvalidOperationException(
                        "Secrets:Providers:AwsSecrets:Identity must be bootstrap, web-identity, or instance-role.");
            }
        }

        // Encrypted database secrets depend on database availability.
        AddEdge(ProviderNode("localvault"), "database");

        foreach (var (name, entry) in bindingsByName)
        {
            var node = SecretNode(name);
            AddNode(node);

            var isDatabaseDestination = entry.ConfigurationKey is { } destination
                && DatabaseKeys.Contains(destination);

            foreach (var dependency in entry.DependsOn)
                AddEdge(node, SecretNode(dependency));

            switch (entry.Provider.ToLowerInvariant())
            {
                case "env":
                case "file":
                    break;

                case "localvault":
                    if (providers.LocalVault?.Enabled != true)
                    {
                        throw new InvalidOperationException(
                            "A secret binding requires localvault, but that provider is not enabled.");
                    }

                    AddEdge(node, ProviderNode("localvault"));
                    break;

                case "hashicorpvault":
                    if (!hasVault)
                    {
                        throw new InvalidOperationException(
                            "A secret binding requires a configured HashiCorpVault endpoint.");
                    }

                    AddEdge(node, ProviderNode("hashicorpvault"));
                    break;

                case "azurekeyvault":
                    if (!hasAzure)
                    {
                        throw new InvalidOperationException(
                            "A secret binding requires a configured AzureKeyVault endpoint.");
                    }

                    AddEdge(node, ProviderNode("azurekeyvault"));
                    break;

                case "awssecrets":
                    if (!hasAws)
                    {
                        throw new InvalidOperationException(
                            "A secret binding requires a configured AwsSecrets region.");
                    }

                    AddEdge(node, ProviderNode("awssecrets"));
                    break;

                default:
                    throw new InvalidOperationException(
                        "A secret binding specifies an unsupported provider.");
            }

            if (isDatabaseDestination)
                AddEdge("database", node);
        }

        // Cycle detection (iterative topological sort).
        var incomingEdges = graph.Keys.ToDictionary(
            n => n,
            _ => 0,
            StringComparer.Ordinal);

        foreach (var dependencies in graph.Values)
        {
            foreach (var dependency in dependencies)
                incomingEdges[dependency]++;
        }

        var ready = new Queue<string>(
            incomingEdges
                .Where(pair => pair.Value == 0)
                .Select(pair => pair.Key));

        var processed = 0;

        while (ready.Count > 0)
        {
            var node = ready.Dequeue();
            processed++;

            foreach (var dependency in graph[node])
            {
                incomingEdges[dependency]--;

                if (incomingEdges[dependency] == 0)
                    ready.Enqueue(dependency);
            }
        }

        if (processed != graph.Count)
        {
            throw new InvalidOperationException(
                "Secret dependency cycle rejected.");
        }
    }

    public static InvalidOperationException Invalid()
        => new("Invalid secret plan, provider configuration or tier dependency.");
}

public sealed class SecretResolutionConfiguration
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

public sealed class SecretCacheOptions
{
    public string Mode { get; set; } = "none";
    public TimeSpan Ttl { get; set; } = TimeSpan.FromSeconds(60);
    public int MaximumEntries { get; set; } = 1024;
    public string OutagePolicy { get; set; } = "use-unexpired-cache";

    public void Validate()
    {
        if (Mode is not ("none" or "memory")
            || Ttl < TimeSpan.FromSeconds(1)
            || Ttl > TimeSpan.FromSeconds(3600)
            || MaximumEntries is < 1 or > 100000
            || OutagePolicy is not ("use-unexpired-cache" or "require-provider"))
        {
            throw SecretsConfiguration.Invalid();
        }
    }
}