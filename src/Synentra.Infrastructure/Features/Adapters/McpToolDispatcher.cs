using System.Text.Json;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration;
using Synentra.Infrastructure.Features.Secrets;

namespace Synentra.Infrastructure.Features.Adapters;

public sealed class McpToolDispatcher(
    IHttpClientFactory clients,
    CredentialProviderRouter credentials,
    IOptions<InfrastructureOptions> options) : IToolDispatcher
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
        if (snapshot.CredentialBinding is { } binding)
            http.DefaultRequestHeaders.Authorization = (await credentials.ResolveAsync(binding, identity, snapshot.OperationId.Value, snapshot.TargetId, ct)).ToAuthHeader();

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
            if (System.Text.Encoding.UTF8.GetByteCount(payload) > options.Value.MaximumResponseBytes)
                return new(ReceiptStatus.Unknown, null, null, null, timestamp, "MCP_RESULT_TOO_LARGE");

            return new(result.IsError == true ? ReceiptStatus.ToolError : ReceiptStatus.Responded,
                null,
                payload,
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
