using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration.DataProtection;
using Synentra.Infrastructure.Configuration.OutboundHttp;
using Synentra.Infrastructure.Features.Secrets;
using System.Text.Json;

namespace Synentra.Infrastructure.Features.Adapters;

public sealed class McpToolDispatcher(
    IHttpClientFactory clients,
    CredentialProviderRouter credentials,
    IOptions<OutboundHttpConfiguration> outboundHttpOptions,
    IOptions<DataProtectionConfiguration> dataProtectionOptions) : IToolDispatcher
{
    public string AdapterId => "mcp";
    public string AdapterVersion => "1.0.0";
    public IReadOnlyList<string> SupportedExecutionModels => new[] { "request_response" };

    public async Task<TransportReceipt> DispatchAsync(AdmittedSnapshot snapshot, ExecutionIdentity identity, CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;
        if (!Uri.TryCreate(snapshot.Path, UriKind.Absolute, out var endpoint))
            return new(ReceiptStatus.ConnectionFailed, null, null, null, timestamp, "INVALID_MCP_ENDPOINT");

        using var http = clients.CreateClient("synentra-governed");
        if (snapshot.CredentialBinding is not null && endpoint.Scheme != "https")
            throw new System.Security.SecurityException("Credentials require HTTPS.");

        ResolvedCredential? resolvedCredential = null;
        if (snapshot.CredentialBinding is { } binding)
        {
            resolvedCredential = await credentials.ResolveAsync(binding, identity, snapshot.OperationId.Value, snapshot.TargetId, ct);
            http.DefaultRequestHeaders.Authorization = resolvedCredential.ToAuthHeader();
        }

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = endpoint,
            TransportMode = HttpTransportMode.StreamableHttp
        }, http);

        try
        {
            await using var client = await McpClient.CreateAsync(transport, cancellationToken: ct);
            var arguments = snapshot.Arguments.ToDictionary(x => x.Key, x => (object?)x.Value, StringComparer.Ordinal);
            var result = await client.CallToolAsync(snapshot.Method, arguments, cancellationToken: ct);
            var payload = JsonSerializer.Serialize(result);
            if (System.Text.Encoding.UTF8.GetByteCount(payload) > (outboundHttpOptions.Value.MaxResponseBodySizeBytes ?? (2 * 1024 * 1024)))
                return new(ReceiptStatus.Unknown, null, null, null, timestamp, "MCP_RESULT_TOO_LARGE");

            return new(result.IsError == true ? ReceiptStatus.ToolError : ReceiptStatus.Responded,
                null,
                CredentialResponseRetention.Apply(payload, snapshot.TargetId, resolvedCredential, dataProtectionOptions.Value),
                null,
                timestamp,
                result.IsError == true ? "MCP_TOOL_ERROR" : null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new(ReceiptStatus.Unknown, null, null, null, timestamp, "MCP_CANCELLED_OR_RESPONSE_LOST");
        }
        catch (OperationCanceledException)
        {
            return new(ReceiptStatus.Unknown, null, null, null, timestamp, "MCP_TIMEOUT_AFTER_POSSIBLE_DISPATCH");
        }
        catch (HttpRequestException)
        {
            return new(ReceiptStatus.ConnectionFailed, null, null, null, timestamp, "MCP_CONNECTION_FAILED");
        }
        catch (Exception)
        {
            return new(ReceiptStatus.Unknown, null, null, null, timestamp, "MCP_UNCERTAIN");
        }
    }
}
