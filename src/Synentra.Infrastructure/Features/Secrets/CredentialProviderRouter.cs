using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Diagnostics;
using Synentra.Persistence.Features.Security;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class CredentialProviderRouter(
    IEnumerable<ICredentialProvider> providers,
    CredentialBindingStore bindings,
    SecretPlan? plan = null, 
    SecretMaterialCache? cache = null, 
    SecretStateStore? states = null) : IRuntimeCredentialResolver
{
    public async Task<ResolvedCredential> ResolveAsync(
        CredentialBinding binding, 
        ExecutionIdentity identity, 
        string operationId, 
        string targetId, 
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(plan?.TimeoutSeconds ?? 30));
        ct = deadline.Token;

        var current = await bindings.GetAsync(binding.Id, ct);
        if (current is null || !current.HasSameAuthority(binding)) 
            throw new UnauthorizedAccessException("Credential authority revoked or replaced.");

        if (plan?.Entries.TryGetValue(binding.Id, out var declaration) == true && declaration.Tier == SecretTier.Runtime && declaration.Reference != current.SecretRef)
            throw new UnauthorizedAccessException("Runtime binding differs from the deployment secret catalog.");

        binding = current; // An explicit material rotation may replace the reference without changing authority.
        if (!SecretPlan.RemoteProviders.Contains(binding.SecretRef.ProviderId)) 
            throw new UnauthorizedAccessException("Runtime source is not permitted.");

        if (binding.AllowedPrincipal != identity.AgentId 
            || binding.DelegationScope != identity.SecurityScope 
            || binding.TargetRef != targetId 
            || !binding.AllowedOperations.Contains(operationId)) 
            throw new UnauthorizedAccessException("Credential binding does not authorize this invocation.");

        var state = states is null ? new SecretState(0, false) : await states.GetAsync(binding.SecretRef, ct);
        if (state.Revoked) 
            throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialAccessRevoked);

        var matches = providers.Where(p => p.ProviderId == binding.SecretRef.ProviderId).ToArray();
        if (matches.Length != 1) 
            throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialProviderMissingOrAmbiguous);

        ResolvedCredential result;
        try
        {
            result = cache is null ? await matches[0].ResolveAsync(binding, ct) : await cache.ResolveAsync(
                new(binding.SecretRef.ProviderId, binding.SecretRef.SecretPath, binding.SecretRef.Version, state.Generation,
                    binding.Id, identity.AgentId, identity.SecurityScope, targetId, operationId), token => matches[0].ResolveAsync(binding, token), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialProviderUnavailable); }

        SecretPayload.Validate(result);

        var after = await bindings.GetAsync(binding.Id, ct);
        if (after is null || after.Version != binding.Version || !after.HasSameAuthority(binding) || after.SecretRef != binding.SecretRef)
            throw new UnauthorizedAccessException("Credential binding changed during resolution.");

        if (states is not null && await states.GetAsync(binding.SecretRef, ct) != state)
            throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialInvalidatedDuringResolution);

        return result;
    }
}
