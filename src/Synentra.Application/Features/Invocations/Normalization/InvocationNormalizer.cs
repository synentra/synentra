using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Invocations;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Application.Common.Serialization;
using Synentra.Application.Features.Manifests.Contracts;

namespace Synentra.Application.Features.Invocations.Normalization;

public sealed class InvocationNormalizer
{
    private readonly IReadOnlyList<IInvocationNormalizer> _normalizers;
    private readonly IManifestProvider _manifests;
    private readonly CanonicalJsonService _canonical;
    private readonly IOperationRegistry _registry;

    public InvocationNormalizer(IEnumerable<IInvocationNormalizer> normalizers, IManifestProvider manifests, CanonicalJsonService canonical, IOperationRegistry registry)
    {
        _normalizers = normalizers.ToList();
        _manifests = manifests;
        _canonical = canonical;
        _registry = registry;
    }

    public async Task<ToolInvocation> NormalizeAsync(
        IntentModality modality, 
        NormalizationInput input, 
        CanonicalPrincipal principal, 
        CancellationToken ct)
    {
        var normalizer = _normalizers.FirstOrDefault(n => n.Modality == modality) ?? throw new InvalidOperationException($"No normalizer for modality {modality}.");
        var invocation = await normalizer.NormalizeAsync(input, principal, ct);
        // Recompute digest centrally
        var digest = _canonical.ComputeDigest(invocation);

        return new ToolInvocation(
            invocation.Id, 
            invocation.SchemaVersion, 
            invocation.AuthenticatedAgentId, 
            invocation.SecurityScope, 
            invocation.OperationId, 
            invocation.OperationVersion, 
            invocation.OperationFamily, 
            invocation.Action, 
            invocation.BindingId, 
            invocation.BindingVersion, 
            invocation.TargetId,
            invocation.Resources,
            invocation.Arguments,
            invocation.Context, 
            invocation.TransportDetails, 
            digest, invocation.Modality, 
            invocation.DeclaredPurpose, 
            invocation.TaskGrantId, 
            invocation.IdempotencyKey, 
            invocation.RequiresGrant,
            invocation.IsMutation, 
            invocation.HasEffectRegistration, 
            invocation.ReceiptTime);
    }
}
