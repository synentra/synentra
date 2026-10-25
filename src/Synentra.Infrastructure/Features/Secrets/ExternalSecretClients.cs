using Amazon.Runtime;
using Amazon.SecretsManager;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using Synentra.Application.Common.Security;
using Synentra.Infrastructure.Configuration.OutboundHttp;
using Synentra.Infrastructure.Configuration.Secrets;
using Synentra.Infrastructure.Networking;

namespace Synentra.Infrastructure.Features.Secrets;

public static class ExternalSecretClients
{
    public static void ValidateEndpoint(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("Secret endpoint must use HTTPS without credentials, query or fragment.");
    }

    public static SecretClient Azure(SecretsConfiguration options)
    {
        ValidateEndpoint(options.Providers.AzureKeyVault.AzureVaultUrl!);
        TokenCredential credential = options.Providers.AzureKeyVault.AzureIdentity switch
        {
            "workload" => new WorkloadIdentityCredential(),
            "managed" => new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned),
            "managed-user" when !string.IsNullOrWhiteSpace(options.Providers.AzureKeyVault.AzureClientId) => new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(options.Providers.AzureKeyVault.AzureClientId)),
            "client-secret" when !string.IsNullOrWhiteSpace(options.Providers.AzureKeyVault.AzureClientSecret) && !string.IsNullOrWhiteSpace(options.Providers.AzureKeyVault.AzureTenantId) && !string.IsNullOrWhiteSpace(options.Providers.AzureKeyVault.AzureClientId) =>
                new ClientSecretCredential(options.Providers.AzureKeyVault.AzureTenantId, options.Providers.AzureKeyVault.AzureClientId, options.Providers.AzureKeyVault.AzureClientSecret),
            _ => throw new InvalidOperationException("Invalid Azure identity configuration.")
        };
        var settings = new SecretClientOptions();
        settings.Retry.MaxRetries = 2;
        settings.Retry.NetworkTimeout = TimeSpan.FromSeconds(10);
        settings.Diagnostics.IsLoggingContentEnabled = false;
        return new SecretClient(new Uri(options.Providers.AzureKeyVault.AzureVaultUrl!), credential, settings);
    }

    public static IAmazonSecretsManager Aws(SecretsConfiguration options)
    {
        AWSCredentials credentials = options.Providers.AwsSecrets.AwsIdentity switch
        {
            "web-identity" => AssumeRoleWithWebIdentityCredentials.FromEnvironmentVariables(),
            "instance-role" when Environment.GetEnvironmentVariable("AWS_EC2_METADATA_V1_DISABLED") == "true" => new InstanceProfileAWSCredentials(),
            "bootstrap" when !string.IsNullOrWhiteSpace(options.Providers.AwsSecrets.AwsAccessKey) && !string.IsNullOrWhiteSpace(options.Providers.AwsSecrets.AwsSecretKey) =>
                string.IsNullOrWhiteSpace(options.Providers.AwsSecrets.AwsSessionToken)
                    ? new BasicAWSCredentials(options.Providers.AwsSecrets.AwsAccessKey, options.Providers.AwsSecrets.AwsSecretKey)
                    : new SessionAWSCredentials(options.Providers.AwsSecrets.AwsAccessKey, options.Providers.AwsSecrets.AwsSecretKey, options.Providers.AwsSecrets.AwsSessionToken),
            _ => throw new InvalidOperationException("Invalid AWS identity configuration; instance roles require IMDSv1 disabled.")
        };
        return new AmazonSecretsManagerClient(credentials, new AmazonSecretsManagerConfig {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Providers.AwsSecrets.AwsRegion), MaxErrorRetry = 2, Timeout = TimeSpan.FromSeconds(10) });
    }

    public static async Task<ResolvedCredential> ReadBootstrapAsync(
        SecretDeclaration entry, 
        SecretsConfiguration options,
        OutboundHttpConfiguration outboundHttpConfiguration, 
        CancellationToken ct)
    {
        var binding = new Synentra.Domain.Security.ValueObjects.CredentialBinding("bootstrap", "host", "host", "host",
            new HashSet<string> { "bootstrap" }, entry.Reference, "bootstrap");
        if (entry.Provider == "azure-keyvault") return await new AzureKeyVaultCredentialProvider(Azure(options)).ResolveAsync(binding, ct);
        if (entry.Provider == "aws-secrets")
        {
            using var client = Aws(options);
            return await new AwsSecretsManagerCredentialProvider(client).ResolveAsync(binding, ct);
        }
        if (entry.Provider == "hashicorp-vault")
        {
            ValidateEndpoint(options.Providers.HashiCorpVault.VaultUrl!);
            using var factory = new BootstrapHttpFactory(outboundHttpConfiguration);
            return await new HashiCorpVaultCredentialProvider(factory, Options.Create(options)).ResolveAsync(binding, ct);
        }
        throw SecretPlan.Invalid();
    }

    private sealed class BootstrapHttpFactory(OutboundHttpConfiguration options) : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient client = new(new GovernedHttpHandler(Options.Create(options))) { Timeout = options.Timeout ?? TimeSpan.FromSeconds(30) };
        public HttpClient CreateClient(string name) => client;
        public void Dispose() => client.Dispose();
    }
}
