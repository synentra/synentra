using System.Text.Json;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Common.Serialization;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Application.Features.Executions.Commands.CancelExecution;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Invocations.Commands.ResumeInvocation;
using Synentra.Application.Features.Invocations.Commands.SubmitInvocation;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Domain.Approvals;
using Synentra.Domain.Execution;
using Synentra.Domain.Governance.Intent;
using Synentra.Mcp;

namespace Synentra.Endpoints.V1;

public sealed class McpGateway : EndpointGroupBase
{
    private const string ProtocolVersion = "2025-11-25";
    private const int Denied = -32070;
    private const int PendingReview = -32071;
    private const int ExpiredAuthorization = -32072;
    private const int AdmissionUnavailable = -32073;

    public override void Map(WebApplication app)
    {
        app.MapPost("/mcp/{serverId}", Post)
            .WithTags("MCP")
            .WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(256 * 1024));
        app.MapDelete("/mcp/{serverId}", Delete).WithTags("MCP");
    }

    private static async Task<IResult> Post(
        string serverId,
        HttpContext context,
        IPrincipalResolver principals,
        IDispatcher dispatcher,
        IRevisionProvider revisions,
        IManifestProvider manifests,
        IApprovalStore approvals,
        IInvocationStore invocations,
        IExecutionRecordStore executions,
        CanonicalJsonService canonical,
        McpSessionRegistry sessions,
        CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(context.Request.Body, new JsonDocumentOptions { MaxDepth = 32 }, ct);
        }
        catch (JsonException)
        {
            return RpcError(null, -32700, "Parse error");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("jsonrpc", out var jsonrpc) || jsonrpc.GetString() != "2.0")
                return RpcError(GetId(root), -32600, "Invalid Request");

            var id = GetId(root);
            if (!root.TryGetProperty("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String)
                return RpcError(id, -32600, "Invalid Request");

            var method = methodElement.GetString()!;
            var parameters = root.TryGetProperty("params", out var p) ? p : default;

            if (method == "initialize")
            {
                var requested = parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("protocolVersion", out var pv) ? pv.GetString() : null;
                if (!string.Equals(requested, ProtocolVersion, StringComparison.Ordinal))
                    return RpcError(id, -32602, $"Unsupported protocol version. Synentra requires {ProtocolVersion}.");

                await revisions.ReloadAsync(ct);
                if (manifests.ActiveBundle?.Spec.Targets.Any(x => x.Id == serverId && x.AdapterRef == "mcp") != true)
                    return RpcError(id, -32602, "Unknown MCP server route.");

                var sessionId = sessions.Create(serverId, principal.AgentId, principal.SecurityScope);
                context.Response.Headers["Mcp-Session-Id"] = sessionId;
                context.Response.Headers["MCP-Protocol-Version"] = ProtocolVersion;
                return RpcResult(id, new
                {
                    protocolVersion = ProtocolVersion,
                    capabilities = new { tools = new { listChanged = false } },
                    serverInfo = new { name = "Synentra", version = "1.0" }
                });
            }

            var sessionIdHeader = context.Request.Headers["Mcp-Session-Id"].ToString();
            if (string.IsNullOrWhiteSpace(sessionIdHeader) || !sessions.Validate(sessionIdHeader, serverId, principal.AgentId, principal.SecurityScope))
                return RpcError(id, -32600, "Missing or invalid MCP session.");

            context.Response.Headers["MCP-Protocol-Version"] = ProtocolVersion;

            if (method is "notifications/initialized")
                return Results.StatusCode(StatusCodes.Status202Accepted);

            if (method == "ping")
                return RpcResult(id, new { });

            if (method == "tools/list")
            {
                await revisions.ReloadAsync(ct);
                var bundle = manifests.ActiveBundle;
                if (bundle is null)
                    return RpcError(id, AdmissionUnavailable, "No active governance bundle.");

                var bindings = bundle.Spec.Bindings
                    .Where(b => b.TargetRef == serverId)
                    .Select(b => new { Binding = b, Operation = bundle.Spec.Operations.SingleOrDefault(o => o.Id == b.OperationId) })
                    .Where(x => x.Operation is not null)
                    .Where(x => !IsHardRestricted(bundle, principal.AgentId, x.Operation!))
                    .Where(x => !string.IsNullOrWhiteSpace(ConfigString(x.Binding.Configuration, "tool")))
                    .OrderBy(x => ConfigString(x.Binding.Configuration, "tool"), StringComparer.Ordinal)
                    .ToArray();

                var offset = 0;
                if (parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("cursor", out var cursor) && cursor.ValueKind == JsonValueKind.String)
                    int.TryParse(cursor.GetString(), out offset);
                offset = Math.Max(offset, 0);
                const int pageSize = 100;
                var page = bindings.Skip(offset).Take(pageSize).ToArray();
                var next = offset + page.Length < bindings.Length ? (offset + page.Length).ToString() : null;

                var tools = page.Select(x => new
                {
                    name = ConfigString(x.Binding.Configuration, "tool")!,
                    description = $"{x.Operation!.Family}:{x.Operation.Action}",
                    inputSchema = x.Operation.InputSchema.ValueKind == JsonValueKind.Undefined
                        ? JsonSerializer.SerializeToElement(new { type = "object" })
                        : x.Operation.InputSchema
                }).ToArray();

                return RpcResult(id, new { tools, nextCursor = next });
            }

            if (method == "notifications/cancelled")
            {
                if (parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("requestId", out var requestId))
                {
                    var requestKey = requestId.GetRawText();
                    if (sessions.TryGetInvocation(sessionIdHeader, requestKey, out var invocationId))
                    {
                        var execution = await executions.FindByInvocationAsync(invocationId, ct);
                        if (execution is not null)
                            await dispatcher.Dispatch(new CancelExecutionCommand(execution.Id, principal), ct);
                    }
                }
                return Results.StatusCode(StatusCodes.Status202Accepted);
            }

            if (method == "tools/call")
            {
                if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                    return RpcError(id, -32602, "Tool name is required.");

                await revisions.ReloadAsync(ct);
                var bundle = manifests.ActiveBundle;
                if (bundle is null)
                    return RpcError(id, AdmissionUnavailable, "No active governance bundle.");

                var toolName = nameElement.GetString()!;
                var candidates = bundle.Spec.Bindings
                    .Where(b => b.TargetRef == serverId && string.Equals(ConfigString(b.Configuration, "tool"), toolName, StringComparison.Ordinal))
                    .ToArray();
                if (candidates.Length != 1)
                    return RpcError(id, -32602, candidates.Length == 0 ? "Unknown tool." : "Ambiguous tool binding.");

                var binding = candidates[0];
                var operation = bundle.Spec.Operations.Single(o => o.Id == binding.OperationId);
                var arguments = parameters.TryGetProperty("arguments", out var a) && a.ValueKind == JsonValueKind.Object
                    ? a.Clone()
                    : JsonSerializer.SerializeToElement(new Dictionary<string, object>());

                var meta = parameters.TryGetProperty("_meta", out var m) && m.ValueKind == JsonValueKind.Object ? m : default;
                var grantId = MetaString(meta, "io.synentra/taskGrantId");
                var idempotencyKey = MetaString(meta, "io.synentra/idempotencyKey") ?? ApiEndpointSupport.GetIdempotencyKey(context.Request);
                var approvalId = MetaString(meta, "io.synentra/approvalId");

                if (!string.IsNullOrWhiteSpace(approvalId))
                {
                    var approval = await approvals.GetAsync(approvalId, ct);
                    if (approval is null || approval.RequestingAgentId != principal.AgentId || approval.SecurityScope != principal.SecurityScope)
                        return RpcError(id, Denied, "Approval is not available for this caller.");
                    if (approval.ExpiresAt <= DateTimeOffset.UtcNow)
                        return RpcError(id, ExpiredAuthorization, "Approval has expired.", new { approvalId });
                    if (approval.BindingId != binding.Id || approval.OperationId != operation.Id)
                        return RpcError(id, Denied, "Approval is bound to a different operation.");

                    var original = await invocations.GetAsync(approval.InvocationId.ToString(), ct);
                    if (original is null)
                        return RpcError(id, Denied, "Approved invocation no longer exists.");
                    var supplied = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments.GetRawText()) ?? new();
                    if (!string.Equals(canonical.CanonicalizeArgumentDigest(original.Arguments), canonical.CanonicalizeArgumentDigest(supplied), StringComparison.Ordinal))
                        return RpcError(id, Denied, "Reviewed arguments do not match the approved invocation.");

                    var existingExecution = await executions.FindByInvocationAsync(original.Id.ToString(), ct);
                    if (existingExecution is not null)
                        return ExecutionResult(id, original.Id.ToString(), existingExecution);

                    if (approval.Status != ApprovalStatus.Approved)
                        return RpcError(id, Denied, $"Approval is {approval.Status.ToString().ToLowerInvariant()}.", new { approvalId });

                    try
                    {
                        var admission = await dispatcher.Dispatch(new ResumeInvocationCommand(original.Id.ToString(), principal), ct);
                        if (!admission.Admitted)
                            return RpcError(id, Denied, "Invocation is no longer admissible.", new { invocationId = original.Id.ToString(), approvalId, reasonCodes = admission.Decision.ReasonCodes });
                    }
                    catch (Synentra.Application.Features.Invocations.Exceptions.AuthorityExpiredException)
                    {
                        return RpcError(id, ExpiredAuthorization, "Approval authority expired.", new { approvalId });
                    }

                    existingExecution = await executions.FindByInvocationAsync(original.Id.ToString(), ct);
                    if (existingExecution is not null)
                        return ExecutionResult(id, original.Id.ToString(), existingExecution);
                    return RpcError(id, AdmissionUnavailable, "Invocation admitted; dispatch is pending.", new { invocationId = original.Id.ToString(), approvalId, dispatchStatus = "claimed" });
                }

                var request = new InvocationRequest
                {
                    OperationId = operation.Id,
                    BindingId = binding.Id,
                    TaskGrantId = grantId,
                    Arguments = arguments
                };

                InvocationResultDto result;
                try
                {
                    result = await dispatcher.Dispatch(new SubmitInvocationCommand(request, principal, idempotencyKey, IntentModality.Mcp), ct);
                }
                catch (InvalidOperationException ex) when (ex.Message == "IDEMPOTENCY_KEY_REQUIRED")
                {
                    return RpcError(id, AdmissionUnavailable, "Mutation tools require io.synentra/idempotencyKey metadata.");
                }

                if (id is not null)
                    sessions.BindRequest(sessionIdHeader, id.Value.GetRawText(), result.InvocationId);

                if (result.Decision.Outcome == "deny")
                    return RpcError(id, Denied, "Synentra denied the tool invocation.", new { result.InvocationId, reasonCodes = result.Decision.ReasonCodes, dispatchStatus = result.Execution.Status });

                if (result.Decision.Outcome == "review")
                    return RpcError(id, PendingReview, "Human review is required.", new { result.InvocationId, approvalId = result.Approval?.Id, dispatchStatus = "not_started" });

                var execution = await executions.FindByInvocationAsync(result.InvocationId, ct);
                if (execution is not null)
                    return ExecutionResult(id, result.InvocationId, execution);

                return RpcError(id, AdmissionUnavailable, "Invocation admitted; dispatch is pending.", new { result.InvocationId, dispatchStatus = result.Execution.Status });
            }

            return RpcError(id, -32601, "Method not found");
        }
    }

    private static async Task<IResult> Delete(
        string serverId,
        HttpContext context,
        IPrincipalResolver principals,
        McpSessionRegistry sessions,
        CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var sessionId = context.Request.Headers["Mcp-Session-Id"].ToString();
        if (string.IsNullOrWhiteSpace(sessionId) || !sessions.Remove(sessionId, serverId, principal.AgentId, principal.SecurityScope))
            return Results.NotFound();
        return Results.NoContent();
    }

    private static IResult ExecutionResult(JsonElement? id, string invocationId, ExecutionRecord execution)
    {
        if (execution.Status == ExecutionStatus.Responded && !string.IsNullOrWhiteSpace(execution.ResponsePayload))
        {
            try
            {
                using var parsed = JsonDocument.Parse(execution.ResponsePayload);
                return RpcResult(id, parsed.RootElement.Clone());
            }
            catch (JsonException)
            {
                return RpcResult(id, new { content = new[] { new { type = "text", text = execution.ResponsePayload } } });
            }
        }

        if (execution.Status == ExecutionStatus.CancelledBeforeDispatch)
            return RpcError(id, AdmissionUnavailable, "Invocation was cancelled before dispatch.", new { invocationId, dispatchStatus = "cancelled_before_dispatch" });
        if (execution.Status == ExecutionStatus.FailedBeforeDispatch)
            return RpcError(id, AdmissionUnavailable, "Invocation failed before dispatch.", new { invocationId, dispatchStatus = "failed_before_dispatch", reason = execution.ErrorMessage });
        if (execution.Status == ExecutionStatus.Unknown)
            return RpcError(id, AdmissionUnavailable, "Dispatch outcome is uncertain; query the invocation before retrying.", new { invocationId, dispatchStatus = "unknown" });

        return RpcError(id, AdmissionUnavailable, "Dispatch is still pending.", new { invocationId, dispatchStatus = execution.Status.ToString().ToLowerInvariant() });
    }


    private static bool IsHardRestricted(Synentra.Application.Features.Manifests.Models.GovernanceBundleModel bundle, string agentId, Synentra.Application.Features.Manifests.Models.OperationModel operation)
    {
        foreach (var restriction in bundle.Spec.Restrictions.Where(r => string.Equals(r.Effect, "deny", StringComparison.OrdinalIgnoreCase)))
        {
            var agentsMatch = restriction.Match.AgentIds is null || restriction.Match.AgentIds.Count == 0 || restriction.Match.AgentIds.Contains(agentId, StringComparer.Ordinal);
            var familiesMatch = restriction.Match.OperationFamilies is null || restriction.Match.OperationFamilies.Count == 0 || restriction.Match.OperationFamilies.Contains(operation.Family, StringComparer.Ordinal);
            var operationsMatch = restriction.Match.OperationIds is null || restriction.Match.OperationIds.Count == 0 || restriction.Match.OperationIds.Contains(operation.Id, StringComparer.Ordinal);
            if (agentsMatch && familiesMatch && operationsMatch)
                return true;
        }
        return false;
    }

    private static string? ConfigString(IReadOnlyDictionary<string, object> configuration, string key)
    {
        if (!configuration.TryGetValue(key, out var value) || value is null) return null;
        if (value is string s) return s;
        if (value is JsonElement e) return e.ValueKind == JsonValueKind.String ? e.GetString() : e.ToString();
        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string? MetaString(JsonElement meta, string name)
        => meta.ValueKind == JsonValueKind.Object && meta.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static JsonElement? GetId(JsonElement root)
        => root.ValueKind == JsonValueKind.Object && root.TryGetProperty("id", out var id) ? id.Clone() : null;

    private static IResult RpcResult(JsonElement? id, object result)
        => Results.Json(new { jsonrpc = "2.0", id = id, result });

    private static IResult RpcError(JsonElement? id, int code, string message, object? data = null)
        => Results.Json(new { jsonrpc = "2.0", id = id, error = new { code, message, data } });
}
