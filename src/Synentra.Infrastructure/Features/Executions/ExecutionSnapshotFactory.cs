using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Domain.Invocations;
using Synentra.Persistence.Features.Security;
using Synentra.Infrastructure.Features.Adapters;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Infrastructure.Features.Executions;
public sealed class ExecutionSnapshotFactory(IManifestProvider manifests, CredentialBindingStore credentials) : IExecutionSnapshotFactory
{
    public async Task<AdmittedSnapshot> CreateAsync(
        ToolInvocation invocation, 
        IReadOnlyList<ResourceDescriptor> resources, 
        CancellationToken ct)
    {
        var bundle = manifests.ActiveBundle ?? throw new InvalidOperationException("No active manifest.");
        var binding = bundle.Spec.Bindings.Single(x => x.Id == invocation.BindingId && x.OperationId == invocation.OperationId.Value);

        if (binding.Version != invocation.BindingVersion || binding.TargetRef != invocation.TargetId)
            throw new InvalidOperationException("Binding changed.");

        var target = bundle.Spec.Targets.Single(x => x.Id == binding.TargetRef);
        var operation = bundle.Spec.Operations.Single(x => x.Id == binding.OperationId);

        if (operation.Execution.Model != "request_response")
            throw new InvalidOperationException("Execution model not supported by installed adapters.");

        string Config(string key, string? fallback = null) => binding.Configuration.GetValueOrDefault(key)?.ToString() ?? target.Configuration.GetValueOrDefault(key)?.ToString() ?? fallback ?? throw new InvalidOperationException("Binding configuration missing: " + key);

        if (target.AdapterRef is not ("http" or "mcp"))
            throw new InvalidOperationException("Unsupported target adapter.");

        var endpoint = Config("endpoint");
        var method = Config(target.AdapterRef == "mcp" ? "tool" : "method", target.AdapterRef == "http" ? "POST" : null);

        var credential = target.CredentialBindingRef is { } id ? await credentials.GetAsync(id, ct) 
            ?? throw new InvalidOperationException("Credential binding missing.") 
            : null;

        return new(invocation.Id, invocation.OperationId, invocation.BindingId, 
            invocation.TargetId, method, endpoint, ArgumentMapper.Map(invocation.Arguments, 
            binding.CanonicalToTarget, resources), new Dictionary<string, string>(), 
            invocation.Digest, invocation.TaskGrantId, invocation.IdempotencyKey, 
            "Idempotency-Key", null, credential, invocation.IsMutation);
    }
}
