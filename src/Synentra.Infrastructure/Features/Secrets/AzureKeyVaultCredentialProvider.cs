using Azure.Security.KeyVault.Secrets;
using Synentra.Application.Common.Security;
using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class AzureKeyVaultCredentialProvider(SecretClient client) : ICredentialProvider
{
    public string ProviderId => "credential/azure-keyvault";

    public async Task<ResolvedCredential> ResolveAsync(CredentialBinding binding, CancellationToken ct)
    {
        if (binding.SecretRef.ProviderId != ProviderId || string.IsNullOrWhiteSpace(binding.SecretRef.Version))
            throw new InvalidOperationException("Secret version required.");

        var response = await client.GetSecretAsync(binding.SecretRef.SecretPath, binding.SecretRef.Version, ct);

        if (response.Value.Properties.Enabled == false || response.Value.Properties.NotBefore > DateTimeOffset.UtcNow)
            throw new CredentialNotFoundException(CredentialDiagnostics.CredentialVersionDisabledOrNotYetValid);

        return new("Bearer", response.Value.Value, response.Value.Properties.ExpiresOn);
    }
}
