using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class CredentialProviderRouter(IEnumerable<ICredentialProvider> providers)
{
    public async Task<ResolvedCredential> ResolveAsync(
        CredentialBinding binding, 
        ExecutionIdentity identity, 
        string operationId, 
        string targetId, 
        CancellationToken ct)
    {
        if (binding.AllowedPrincipal != identity.AgentId || binding.DelegationScope != identity.SecurityScope || binding.TargetRef != targetId || !binding.AllowedOperations.Contains(operationId))
            throw new UnauthorizedAccessException("Credential binding does not authorize this invocation.");

        var matches = providers.Where(p => p.ProviderId == binding.SecretRef.ProviderId).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Credential provider missing or ambiguous.");

        var result = await matches[0].ResolveAsync(binding, ct);
        if (result.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new Exception("Credential expired.");

        return result;
    }
}
