using System.Text.Json;
using Microsoft.Extensions.Options;
using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration.Secrets;
using Synentra.Infrastructure.Diagnostics;
using Synentra.Infrastructure.Networking;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class HashiCorpVaultCredentialProvider(IHttpClientFactory clients, IOptions<SecretsConfiguration> options) : ICredentialProvider
{
    public string ProviderId => "hashicorp-vault";

    public async Task<ResolvedCredential> ResolveAsync(CredentialBinding binding, CancellationToken ct)
    {
        var o = options.Value.Providers.HashiCorpVault;
        if (!Uri.TryCreate(o.VaultUrl, UriKind.Absolute, out var baseUri) 
            || baseUri.Scheme != "https" 
            || !string.IsNullOrEmpty(baseUri.UserInfo) 
            || !string.IsNullOrEmpty(baseUri.Query) 
            || string.IsNullOrWhiteSpace(o.VaultToken) 
            || o.VaultToken.Any(char.IsControl))
            throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialBootstrapConfigurationInvalid);

        if (binding.SecretRef.SecretPath.Split('/').Any(s => s is "" or "." or ".."))
            throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialInvalidVaultPath);

        if (binding.SecretRef.ProviderId != ProviderId 
            || !int.TryParse(binding.SecretRef.Version, out var version) 
            || version < 1)
            throw new InvalidOperationException("Vault requires an explicit KV v2 version.");

        var path = string.Join('/', binding.SecretRef.SecretPath.Split('/').Select(Uri.EscapeDataString));
        var uri = new Uri(new Uri(o.VaultUrl!), $"v1/{Uri.EscapeDataString(o.VaultMount)}/data/{path}?version={version}");
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("X-Vault-Token", o.VaultToken);
        using var response = await clients.CreateClient("synentra-governed").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await BoundedResponse.ReadAsync(response.Content, 65536, ct));
        var data = doc.RootElement.GetProperty("data").GetProperty("data");

        var value = data.GetProperty("value").GetString() 
            ?? throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialValueMissing);

        var scheme = data.TryGetProperty("scheme", out var s) ? s.GetString()! : "Bearer";
        DateTimeOffset? expires = data.TryGetProperty("expiresAt", out var e) && e.ValueKind == JsonValueKind.String ? e.GetDateTimeOffset() : null;
        return new(scheme, value, expires, scheme);
    }
}
