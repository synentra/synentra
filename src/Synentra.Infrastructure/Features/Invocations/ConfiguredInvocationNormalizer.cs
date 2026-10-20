using Synentra.Application.Features.Invocations.Normalization;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Domain.Invocations;
using Synentra.Domain.Invocations.ValueObjects;
using Synentra.Domain.Operations.ValueObjects;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Resources.ValueObjects;
using Synentra.Infrastructure.Features.Adapters;

namespace Synentra.Infrastructure.Features.Invocations;

public sealed class ConfiguredInvocationNormalizer(IManifestProvider manifests, IRevisionProvider revisions, IntentModality modality) : IInvocationNormalizer
{
    public IntentModality Modality => modality;

    public async Task<ToolInvocation> NormalizeAsync(NormalizationInput input, CanonicalPrincipal principal, CancellationToken ct)
    {
        await revisions.ReloadAsync(ct);
        var bundle = manifests.ActiveBundle ?? throw new InvalidOperationException("NO_ACTIVE_BUNDLE");
        var binding = bundle.Spec.Bindings.Single(x => x.Id == input.BindingId && x.OperationId == input.OperationId);
        var operation = bundle.Spec.Operations.Single(x => x.Id == input.OperationId);

        if (operation.Execution.Model != "request_response")
            throw new InvalidOperationException("Unsupported execution model.");

        var arguments = modality switch
        {
            IntentModality.Mcp => ArgumentMapper.Map(
                input.Arguments,
                binding.TargetToCanonical,
                Array.Empty<ResourceDescriptor>()),

            _ => input.Arguments
        };

        //var arguments = ArgumentMapper.Map(input.Arguments, binding.TargetToCanonical, Array.Empty<ResourceDescriptor>());
        return new(new InvocationId(
            Guid.NewGuid()), 
            1, 
            principal.AgentId, 
            principal.SecurityScope, 
            new OperationId(operation.Id), 
            operation.Version, 
            operation.Family,
            operation.Action, 
            binding.Id, 
            binding.Version, 
            binding.TargetRef, 
            Array.Empty<ResourceDescriptor>(), 
            arguments, 
            input.Context, 
            new Dictionary<string, string>(), 
            new BindingDigest(new string ('0', 64)), 
            modality,
            input.DeclaredPurpose, 
            input.TaskGrantId,
            input.IdempotencyKey, 
            operation.TaskGrantRequired, 
            operation.Mutability != "read_only", 
            operation.Effect is not null, 
            DateTimeOffset.UtcNow);
    }
}
