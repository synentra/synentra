using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Domain.Invocations;
using Synentra.Persistence.Features.Security;
using Synentra.Infrastructure.Features.Adapters;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Infrastructure.Features.Executions;
public sealed class ExecutionSnapshotFactory(
    IManifestProvider manifests, 
    CredentialBindingStore credentials,
    IRevisionProvider revisions) : IExecutionSnapshotFactory
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

        if (target.AdapterRef is not ("adapter/http" or "adapter/mcp"))
            throw new InvalidOperationException("Unsupported target adapter.");

        var endpoint = AdapterConfiguration.String(target.Configuration, "endpoint")
            ?? throw new InvalidOperationException("Target configuration missing endpoint.");

        string method;
        string path;
        IReadOnlyDictionary<string, string> headers;
        string? idempotencyHeader = null;

        if (target.AdapterRef == "adapter/http")
        {
            method = AdapterConfiguration.String(binding.Configuration, "method") ?? "POST";
            var bindingPath = AdapterConfiguration.String(binding.Configuration, "path");
            path = AdapterConfiguration.BuildAbsoluteEndpoint(endpoint, bindingPath);
            headers = AdapterConfiguration.StringMap(binding.Configuration, "headers");

            if (binding.Configuration.TryGetValue("idempotency", out var idempotency) && idempotency is not null)
            {
                var map = idempotency is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Object
                    ? je.EnumerateObject().ToDictionary(x => x.Name, x => (object)x.Value)
                    : idempotency as IReadOnlyDictionary<string, object>;
                if (map is not null)
                    idempotencyHeader = AdapterConfiguration.String(map, "name");
            }

            if (invocation.IsMutation && string.IsNullOrWhiteSpace(idempotencyHeader))
                idempotencyHeader = "Idempotency-Key";
        }
        else
        {
            method = AdapterConfiguration.String(binding.Configuration, "tool")
                ?? throw new InvalidOperationException("MCP binding configuration missing tool.");
            path = endpoint;
            headers = new Dictionary<string, string>();
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
            (!string.Equals(endpointUri.Scheme, "http", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(endpointUri.Scheme, "https", StringComparison.OrdinalIgnoreCase)) ||
            string.IsNullOrWhiteSpace(endpointUri.Host) ||
            !string.IsNullOrEmpty(endpointUri.UserInfo) ||
            !string.IsNullOrEmpty(endpointUri.Fragment))
            throw new InvalidOperationException("Target endpoint must be a valid HTTP(S) URI without user information.");

        if (!Uri.TryCreate(path, UriKind.Absolute, out var requestUri) ||
            !string.IsNullOrEmpty(requestUri.UserInfo) ||
            !string.IsNullOrEmpty(requestUri.Fragment))
            throw new InvalidOperationException("Target request must be a valid absolute URI without user information or fragments.");

        var credential = target.CredentialBindingRef is { } id
            ? await credentials.GetAsync(id, ct) ?? throw new InvalidOperationException("Credential binding missing.")
            : null;
        if (credential is not null && (credential.AllowedPrincipal != invocation.AuthenticatedAgentId ||
            credential.DelegationScope != invocation.SecurityScope || credential.TargetRef != invocation.TargetId ||
            !credential.AllowedOperations.Contains(invocation.OperationId.Value)))
            throw new UnauthorizedAccessException("Credential binding does not authorize admission.");

        var authorizedTarget = new AuthorizedHttpTarget(
            target.Id,
            endpointUri,
            requestUri,
            revisions.ActiveRevision,
            credential is not null);

        return new AdmittedSnapshot(
            invocation.Id,
            invocation.OperationId,
            invocation.BindingId,
            invocation.TargetId,
            authorizedTarget,
            method,
            path,
            ArgumentMapper.Map(invocation.Arguments, binding.CanonicalToTarget, resources),
            headers,
            invocation.Digest,
            invocation.TaskGrantId,
            invocation.IdempotencyKey,
            idempotencyHeader,
            null,
            credential,
            invocation.IsMutation);
    }
}
