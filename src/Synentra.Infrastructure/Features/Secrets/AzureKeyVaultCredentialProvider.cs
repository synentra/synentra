using Azure.Security.KeyVault.Secrets;
using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class AzureKeyVaultCredentialProvider(SecretClient client) : ICredentialProvider
{
    public string ProviderId => "azure-keyvault";

    public async Task<ResolvedCredential> ResolveAsync(CredentialBinding binding, CancellationToken ct)
    {
        if (binding.SecretRef.ProviderId != ProviderId || string.IsNullOrWhiteSpace(binding.SecretRef.Version))
            throw new InvalidOperationException("Secret version required.");

        var response = await client.GetSecretAsync(binding.SecretRef.SecretPath, binding.SecretRef.Version, ct);
        return new("Bearer", response.Value.Value, response.Value.Properties.ExpiresOn);
    }
}
