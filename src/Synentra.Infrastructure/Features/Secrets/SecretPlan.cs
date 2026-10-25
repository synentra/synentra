using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration.OutboundHttp;
using Synentra.Infrastructure.Configuration.Secrets;
using Synentra.Persistence.EntityConfigurations;

namespace Synentra.Infrastructure.Features.Secrets;

public enum SecretTier { Bootstrap = 0, Startup = 1, Runtime = 2 }

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

public sealed class SecretPlan
{
    public Dictionary<string, SecretDeclaration> Entries { get; set; } = new(StringComparer.Ordinal);

    public string? DefaultProvider { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    public SecretCacheOptions Cache { get; set; } = new();

    public bool IsDevelopment { get; private set; }

    internal void SetEnvironment(bool development) => IsDevelopment = development;

    public static readonly HashSet<string> RemoteProviders = ["encrypted-db", "hashicorp-vault", "azure-keyvault", "aws-secrets"];

    public static readonly HashSet<string> DatabaseKeys = ["System:Storage:Database:Providers:Sqlite:ConnectionString", "System:Storage:Database:Providers:Postgres:ConnectionString"];

    public static readonly HashSet<string> MetadataKeys = ["Synentra:Infrastructure:KeyRingPath", "Synentra:Infrastructure:KeyRingCertificatePath"];

    public static readonly HashSet<string> BootstrapKeys = [
        "System:Storage:Database:Providers:Sqlite:ConnectionString", "System:Storage:Database:Providers:Postgres:ConnectionString",
        "Synentra:Infrastructure:KeyRingPath", "Synentra:Infrastructure:KeyRingCertificatePath",
        "Synentra:Infrastructure:KeyRingCertificatePassword", "Synentra:Secrets:VaultToken",
        "Synentra:Secrets:AwsAccessKey", "Synentra:Secrets:AwsSecretKey", "Synentra:Secrets:AwsSessionToken", "Synentra:Secrets:AzureClientSecret"
    ];

    public static readonly HashSet<string> StartupKeys = ["Synentra:Identity:LocalSigningKeyBase64", "Synentra:Identity:LdapServicePassword", "Synentra:Infrastructure:OpaBearerToken"];

    public static bool IsStartupKey(string key) 
        => StartupKeys.Contains(key) 
        || (key.StartsWith("Synentra:Identity:LocalValidationKeys:", StringComparison.Ordinal) 
        && System.Text.RegularExpressions.Regex.IsMatch(key["Synentra:Identity:LocalValidationKeys:".Length..], "^[A-Za-z0-9_-]{1,64}$"));

    public void Validate()
    {
        if (TimeoutSeconds is < 1 or > 300 || Entries is null) 
            throw Invalid();

        Cache.Validate();
        foreach (var (name, entry) in Entries)
        {
            if (string.IsNullOrWhiteSpace(name) 
                || entry is null 
                || !Enum.IsDefined(entry.Tier) 
                || string.IsNullOrWhiteSpace(entry.Path)) 
                throw Invalid();

            var local = entry.Provider is "env" or "file";
            var databaseBootstrap = entry.Tier == SecretTier.Bootstrap 
                && DatabaseKeys.Contains(entry.ConfigurationKey ?? "") 
                && entry.Provider is "hashicorp-vault" or "azure-keyvault" or "aws-secrets";

            if (entry.Tier == SecretTier.Bootstrap && !local && !databaseBootstrap ||
                entry.Tier == SecretTier.Runtime && !RemoteProviders.Contains(entry.Provider) ||
                entry.Tier == SecretTier.Startup && !local && !RemoteProviders.Contains(entry.Provider)) 
                throw Invalid();

            if (!local && string.IsNullOrWhiteSpace(entry.Version)) 
                throw Invalid();

            if (entry.Tier == SecretTier.Runtime && entry.ConfigurationKey is not null) 
                throw Invalid();

            if (entry.Tier != SecretTier.Runtime && (entry.ConfigurationKey is null ||
                !(entry.Tier == SecretTier.Bootstrap ? BootstrapKeys.Contains(entry.ConfigurationKey) : IsStartupKey(entry.ConfigurationKey)))) 
                throw Invalid();

            foreach (var dependency in entry.DependsOn)
            {
                if (!Entries.TryGetValue(dependency, out var parent)) 
                    throw Invalid();

                if (parent.Tier >= entry.Tier && !(databaseBootstrap && parent.Tier == SecretTier.Bootstrap && parent.Provider is "env" or "file")) 
                    throw Invalid();
            }
        }
        var keys = Entries.Values
            .Where(e => e.ConfigurationKey is not null)
            .Select(e => e.ConfigurationKey!)
            .ToArray();

        if (keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != keys.Length) 
            throw Invalid();
    }

    /// <summary>Builds the graph from the same destinations and provider settings used by resolution.</summary>
    public void ValidateDependencies(IConfiguration configuration)
    {
        Validate();
        var options = configuration.GetSection("Secrets").Get<SecretsConfiguration>() ?? new();
        var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        void Edge(string node, string dependency) { if (!graph.TryGetValue(node, out var edges)) graph[node] = edges = []; edges.Add(dependency); }
        string? Destination(string key) => Entries.SingleOrDefault(x => x.Value.ConfigurationKey == key).Key;

        void Root(string provider, string key)
        {
            var name = Destination(key);
            if (name is null)
            {
                if (!IsDevelopment || string.IsNullOrWhiteSpace(configuration[key])) 
                    throw Invalid();
                return;
            }

            var entry = Entries[name];
            if (entry.Tier != SecretTier.Bootstrap || entry.Provider is not ("env" or "file")) 
                throw Invalid();

            Edge("provider:" + provider, "secret:" + name);
        }

        if (options.Providers.HashiCorpVault.VaultUrl is not null) { 
            ExternalSecretClients.ValidateEndpoint(options.Providers.HashiCorpVault.VaultUrl); Root("hashicorp-vault", "Secrets:VaultToken"); 
        }

        if (options.Providers.HashiCorpVault.VaultUrl is not null)
        {
            ExternalSecretClients.ValidateEndpoint(options.Providers.AzureKeyVault.AzureVaultUrl);

            if (options.Providers.AzureKeyVault.AzureIdentity == "client-secret") 
                Root("azure-keyvault", "Secrets:AzureClientSecret");

            else if (options.Providers.AzureKeyVault.AzureIdentity is not ("workload" or "managed" or "managed-user")) 
                throw Invalid();
        }
        if (options.Providers.AwsSecrets.AwsRegion is not null)
        {
            if (options.Providers.AwsSecrets.AwsIdentity == "bootstrap") { 
                Root("aws-secrets", "Secrets:AwsAccessKey"); Root("aws-secrets", "Secrets:AwsSecretKey"); 
            }
            else if (options.Providers.AwsSecrets.AwsIdentity is not ("web-identity" or "instance-role")) 
                throw Invalid();
        }

        Edge("provider:encrypted-db", "database");
        foreach (var (name, entry) in Entries)
        {
            foreach (var dependency in entry.DependsOn) 
                Edge("secret:" + name, "secret:" + dependency);

            if (RemoteProviders.Contains(entry.Provider))
            {
                if (entry.Provider == "encrypted-db" && !options.Providers.EncryptedDatabase.EncryptedDatabaseEnabled 
                    || entry.Provider == "hashicorp-vault" && options.Providers.HashiCorpVault.VaultUrl is null 
                    || entry.Provider == "azure-keyvault" && options.Providers.AzureKeyVault.AzureVaultUrl is null 
                    || entry.Provider == "aws-secrets" && options.Providers.AwsSecrets.AwsRegion is null) 
                    throw Invalid();

                Edge("secret:" + name, "provider:" + entry.Provider);
            }
            if (DatabaseKeys.Contains(entry.ConfigurationKey ?? "")) 
                Edge("database", "secret:" + name);
        }
        var visiting = new HashSet<string>(); var visited = new HashSet<string>();
        void Visit(string node)
        {
            if (visited.Contains(node)) return;
            if (!visiting.Add(node)) 
                throw new InvalidOperationException("Secret dependency cycle rejected.");

            if (graph.TryGetValue(node, out var dependencies)) 
                foreach (var dependency in dependencies) 
                    Visit(dependency);

            visiting.Remove(node); visited.Add(node);
        }
        foreach (var node in graph.Keys.ToArray()) 
            Visit(node);
    }

    public static InvalidOperationException Invalid() => new("Invalid secret plan, provider configuration or tier dependency.");
}

public static class BootstrapSecrets
{
    public static SecretPlan Apply(ConfigurationManager configuration)
    {
        var plan = Prepare(configuration, false);
        if (plan.Entries.Values.Any(e => e.Tier == SecretTier.Bootstrap && e.Provider is not ("env" or "file")))
            throw new InvalidOperationException("External database bootstrap requires ApplyAsync.");

        ApplyLocal(configuration, plan);
        return plan;
    }

    public static async Task<SecretPlan> ApplyAsync(
        ConfigurationManager configuration, 
        bool development, 
        CancellationToken ct,
        Func<SecretDeclaration, CancellationToken, Task<ResolvedCredential>>? externalReader = null)
    {
        var plan = Prepare(configuration, development);
        ApplyLocal(configuration, plan);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(plan.TimeoutSeconds));
        var options = configuration.GetSection("Secrets").Get<SecretsConfiguration>() ?? new();
        var outboundHttpConfiguration = configuration.GetSection("OutboundHttp").Get<OutboundHttpConfiguration>() ?? new();
        var values = new Dictionary<string, string?>();
        foreach (var entry in plan.Entries.Values.Where(e => e.Tier == SecretTier.Bootstrap && e.Provider is not ("env" or "file")))
        {
            try
            {
                var result = externalReader is null ? await ExternalSecretClients.ReadBootstrapAsync(entry, options, outboundHttpConfiguration, deadline.Token)
                    : await externalReader(entry, deadline.Token);
                SecretPayload.Validate(result);
                values.Add(entry.ConfigurationKey!, result.Value);
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested) { throw; }
            catch { throw new InvalidOperationException("External database bootstrap failed."); }
        }
        configuration.AddInMemoryCollection(values);
        return plan;
    }

    private static SecretPlan Prepare(ConfigurationManager configuration, bool development)
    {
        var plan = configuration.GetSection("Synentra:SecretPlan").Get<SecretPlan>() ?? new();
        plan.SetEnvironment(development);
        if (!development && configuration.GetValue<bool?>("Synentra:Infrastructure:RequireEncryptedKeyRing") == false) throw new InvalidOperationException("Production and maintenance require an encrypted key ring.");
        var configured = new List<string>();
        if (configuration["Synentra:Secrets:VaultUrl"] is not null) configured.Add("hashicorp-vault");
        if (configuration["Synentra:Secrets:AzureVaultUrl"] is not null) configured.Add("azure-keyvault");
        if (configuration["Synentra:Secrets:AwsRegion"] is not null) configured.Add("aws-secrets");
        plan.DefaultProvider ??= configured.Count == 1 ? configured[0] : null;
        foreach (var entry in plan.Entries.Values) if (string.IsNullOrEmpty(entry.Provider)) entry.Provider = plan.DefaultProvider ?? throw SecretPlan.Invalid();
        plan.ValidateDependencies(configuration);
        if (!development)
        {
            foreach (var key in SecretPlan.BootstrapKeys)
                if (!SecretPlan.MetadataKeys.Contains(key) && !string.IsNullOrWhiteSpace(configuration[key]) &&
                    !(SecretPlan.DatabaseKeys.Contains(key) && IsNonsecretConnection(configuration[key]!)))
                    throw new InvalidOperationException("Production secret values must use declared references.");
            foreach (var key in SecretPlan.StartupKeys.Concat(configuration.GetSection("Synentra:Identity:LocalValidationKeys").GetChildren().Select(x => x.Path)))
                if (!string.IsNullOrWhiteSpace(configuration[key])) throw new InvalidOperationException("Production startup secrets must use declared references.");
        }
        if (!string.IsNullOrWhiteSpace(configuration["Security:AgentAuth:TokenIssuance:Secret"])) throw new InvalidOperationException("Legacy signing secrets are not supported.");
        var database = (configuration["System:Storage:Database:DefaultProvider"] ?? "sqlite").ToLowerInvariant();
        var connectionKey = database switch { "sqlite" => "System:Storage:Database:Providers:Sqlite:ConnectionString", "postgres" or "postgresql" => "System:Storage:Database:Providers:Postgres:ConnectionString", _ => throw SecretPlan.Invalid() };
        if (!plan.Entries.Values.Any(e => e.Tier == SecretTier.Bootstrap && e.ConfigurationKey == connectionKey) && string.IsNullOrWhiteSpace(configuration[connectionKey])) throw SecretPlan.Invalid();
        if (!plan.Entries.Values.Any(e => e.ConfigurationKey == "Synentra:Infrastructure:KeyRingPath") && string.IsNullOrWhiteSpace(configuration["Synentra:Infrastructure:KeyRingPath"])) throw SecretPlan.Invalid();
        if (configuration.GetValue<bool>("Observability:Logging:Seq:Enabled") || configuration.GetValue<bool>("Observability:Logging:Otlp:Enabled") || configuration.GetValue<bool>("Observability:OpenTelemetry:Enabled")) throw new InvalidOperationException("Requested observability exporter is not implemented in this host.");
        if (!string.IsNullOrWhiteSpace(configuration["Observability:Logging:Seq:ApiKey"])) throw new InvalidOperationException("Seq exporter is not implemented in this host.");
        return plan;
    }

    private static bool IsNonsecretConnection(string value)
    {
        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = value };
            return !builder.Keys.Cast<string>().Any(k => (k.Contains("password", StringComparison.OrdinalIgnoreCase) || k.Equals("pwd", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("token", StringComparison.OrdinalIgnoreCase) || k.Contains("secret", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(builder[k]?.ToString()));
        }
        catch { return false; }
    }

    private static void ApplyLocal(ConfigurationManager configuration, SecretPlan plan)
    {
        configuration.AddInMemoryCollection(plan.Entries.Values.Where(e => e.Tier == SecretTier.Bootstrap && e.Provider is "env" or "file")
            .ToDictionary(e => e.ConfigurationKey!, e => (string?)Read(e)));
    }

    public static string Read(SecretDeclaration entry)
    {
        try
        {
            string? value;
            if (entry.Provider == "env") value = Environment.GetEnvironmentVariable(entry.Path);
            else if (entry.Provider == "file")
            {
                KeyRingSecurity.ValidateSecretFile(entry.Path);
                if (new FileInfo(entry.Path).Length > 65536) throw SecretPlan.Invalid();
                value = File.ReadAllText(entry.Path).TrimEnd('\r', '\n');
            }
            else throw SecretPlan.Invalid();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 65536 || value.Any(char.IsControl)) throw SecretPlan.Invalid();
            return value;
        }
        catch { throw new InvalidOperationException("Bootstrap secret unavailable or invalid."); }
    }
}



