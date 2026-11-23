using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Synentra.Application.Common.Security;
using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class AwsSecretsManagerCredentialProvider(IAmazonSecretsManager client) : ICredentialProvider
{
    public string ProviderId => "credential/aws-secrets";

    public async Task<ResolvedCredential> ResolveAsync(CredentialBinding binding, CancellationToken ct)
    {
        if (binding.SecretRef.ProviderId != ProviderId || string.IsNullOrWhiteSpace(binding.SecretRef.Version))
            throw new InvalidOperationException("Secret version required.");

        var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = binding.SecretRef.SecretPath, VersionId = binding.SecretRef.Version }, ct);
        return new("Bearer", response.SecretString 
            ?? throw new CredentialNotFoundException(CredentialDiagnostics.CredentialBinarySecretsNotAuthenticationHeaders), null);
    }
}
