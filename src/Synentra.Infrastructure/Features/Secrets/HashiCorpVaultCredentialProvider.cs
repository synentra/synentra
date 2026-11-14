using Microsoft.Extensions.Options;
using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration.Secrets;
using Synentra.Infrastructure.Diagnostics;
using Synentra.Infrastructure.Networking;
using System.Globalization;
using System.Text.Json;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class HashiCorpVaultCredentialProvider(
    IHttpClientFactory clients,
    IOptions<SecretsConfiguration> options) : ICredentialProvider
{
    private const int MaximumResponseBytes = 65_536;

    public string ProviderId => "credential/hashicorp-vault";

    public async Task<ResolvedCredential> ResolveAsync(
        CredentialBinding binding,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(binding.SecretRef);

        var secretRef = binding.SecretRef;
        var configuration = options.Value.Providers.HashiCorpVault;

        if (!string.Equals(
                secretRef.ProviderId,
                ProviderId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"This provider only supports '{ProviderId}'.");
        }

        // Explicit version pinning is a Synentra requirement.
        if (!int.TryParse(
                secretRef.Version,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var version) ||
            version < 1)
        {
            throw new InvalidOperationException(
                "Vault requires an explicit positive KV v2 version.");
        }

        if (string.IsNullOrWhiteSpace(secretRef.Field) ||
            secretRef.Field.Any(char.IsControl))
        {
            throw new ArgumentException(
                "A non-empty Vault secret field is required.",
                nameof(binding));
        }

        if (!Uri.TryCreate(
                configuration.Endpoint,
                UriKind.Absolute,
                out var baseUri) ||
            !IsAllowedVaultUri(baseUri) ||
            string.IsNullOrWhiteSpace(configuration.Token) ||
            configuration.Token.Any(char.IsControl) ||
            !IsValidPath(configuration.Mount))
        {
            throw new CredentialNotFoundException(
                InfrastructureErrors.Credential
                    .CredentialBootstrapConfigurationInvalid);
        }

        if (!IsValidPath(secretRef.SecretPath))
        {
            throw new CredentialNotFoundException(
                InfrastructureErrors.Credential.CredentialInvalidVaultPath);
        }

        // Preserve a configured base-path prefix, with or without
        // a trailing slash.
        var normalizedBaseUri = new Uri(
            baseUri.AbsoluteUri.TrimEnd('/') + "/",
            UriKind.Absolute);

        var mount = EscapePath(configuration.Mount);
        var path = EscapePath(secretRef.SecretPath);
        var versionText = version.ToString(CultureInfo.InvariantCulture);

        var requestUri = new Uri(
            normalizedBaseUri,
            $"v2/{mount}/data/{path}?version={versionText}");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            requestUri);

        request.Headers.Add(
            "X-Vault-Token",
            configuration.Token);

        request.Headers.Accept.ParseAdd("application/json");

        using var client = clients.CreateClient("synentra-vault");

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        response.EnsureSuccessStatusCode();

        var responseBody = await BoundedResponse.ReadAsync(
            response.Content,
            MaximumResponseBytes,
            ct);

        using var document = JsonDocument.Parse(responseBody);

        var secretData = GetSecretData(document.RootElement);

        var value = GetRequiredString(secretData, secretRef.Field);
        var scheme = GetScheme(secretData);
        var expiresAt = GetExpiresAt(secretData);

        // Retains your existing ResolvedCredential constructor usage.
        return new ResolvedCredential(
            scheme,
            value,
            expiresAt,
            scheme);
    }

    private static bool IsAllowedVaultUri(Uri uri)
    {
        if (!string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        // HTTPS for remote Vault servers.
        // HTTP is permitted only for local development.
        return uri.Scheme == Uri.UriSchemeHttps ||
               (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
    }

    private static bool IsValidPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            path.Any(char.IsControl) ||
            path.Contains('\\'))
        {
            return false;
        }

        // Require relative paths without empty or traversal segments.
        return path.Split('/').All(segment =>
            !string.IsNullOrWhiteSpace(segment) &&
            segment is not "." and not "..");
    }

    private static string EscapePath(string path)
    {
        // Escape each segment separately so nested paths remain valid.
        return string.Join(
            "/",
            path.Split('/').Select(Uri.EscapeDataString));
    }

    private static JsonElement GetSecretData(JsonElement root)
    {
        // KV v2 response envelope: data.data.
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("data", out var envelope) ||
            envelope.ValueKind != JsonValueKind.Object ||
            !envelope.TryGetProperty("data", out var secretData) ||
            secretData.ValueKind != JsonValueKind.Object)
        {
            throw new CredentialNotFoundException(
                InfrastructureErrors.Credential.CredentialValueMissing);
        }

        return secretData;
    }

    private static string GetRequiredString(
        JsonElement secretData,
        string field)
    {
        if (!secretData.TryGetProperty(field, out var element) ||
            element.ValueKind != JsonValueKind.String)
        {
            throw new CredentialNotFoundException(
                InfrastructureErrors.Credential.CredentialValueMissing);
        }

        var value = element.GetString();

        if (string.IsNullOrEmpty(value))
        {
            throw new CredentialNotFoundException(
                InfrastructureErrors.Credential.CredentialValueMissing);
        }

        // Preserve the secret exactly; do not trim it.
        return value;
    }

    private static string GetScheme(JsonElement secretData)
    {
        if (!secretData.TryGetProperty("scheme", out var element))
        {
            return "Bearer";
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                "Vault credential 'scheme' must be a string.");
        }

        var scheme = element.GetString();

        if (string.IsNullOrWhiteSpace(scheme) ||
            scheme.Any(char.IsControl))
        {
            throw new InvalidOperationException(
                "Vault credential 'scheme' must be non-empty " +
                "and contain no control characters.");
        }

        return scheme;
    }

    private static DateTimeOffset? GetExpiresAt(JsonElement secretData)
    {
        if (!secretData.TryGetProperty("expiresAt", out var element) ||
            element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.String ||
            !element.TryGetDateTimeOffset(out var expiresAt))
        {
            throw new InvalidOperationException(
                "Vault credential 'expiresAt' must be a valid " +
                "ISO 8601 timestamp.");
        }

        return expiresAt;
    }
}