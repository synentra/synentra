using System.Net.Http.Json;
using System.Security;
using System.Text;
using Microsoft.Extensions.Options;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Networking;
using Synentra.Infrastructure.Features.Secrets;
using Synentra.Infrastructure.Configuration.OutboundHttp;

namespace Synentra.Infrastructure.Features.Adapters;

public sealed class HttpToolDispatcher(
    IHttpClientFactory clients,
    CredentialProviderRouter credentials,
    IOptions<OutboundHttpConfiguration> options) : IToolDispatcher
{
    private static readonly HashSet<string> SafeHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept",
        "If-Match",
        "If-None-Match",
        "Prefer",
        "User-Agent"
    };

    public string AdapterId => "http";
    public string AdapterVersion => "1.0.0";
    public IReadOnlyList<string> SupportedExecutionModels => new[] { "request_response" };

    public async Task<TransportReceipt> DispatchAsync(AdmittedSnapshot snapshot, ExecutionIdentity identity, CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;

        if (!Uri.TryCreate(snapshot.Path, UriKind.Absolute, out var destination))
            return new(ReceiptStatus.ConnectionFailed, null, null, null, timestamp, "INVALID_REGISTERED_DESTINATION");

        using var request = new HttpRequestMessage(new HttpMethod(snapshot.Method), destination);

        foreach (var (key, value) in snapshot.Headers)
        {
            if (!SafeHeaders.Contains(key))
                throw new SecurityException($"Configured header '{key}' is not in the safe outbound header subset.");

            if (!request.Headers.TryAddWithoutValidation(key, value))
                throw new InvalidOperationException($"Configured header '{key}' could not be applied.");
        }

        if (snapshot.CredentialBinding is { } binding)
            request.Headers.Authorization = (await credentials.ResolveAsync(binding, identity, snapshot.OperationId.Value, snapshot.TargetId, ct)).ToAuthHeader();

        if (!string.IsNullOrWhiteSpace(snapshot.IdempotencyKey))
        {
            var header = snapshot.IdempotencyHeader ?? "Idempotency-Key";
            if (!header.Equals("Idempotency-Key", StringComparison.OrdinalIgnoreCase))
                throw new SecurityException("Only the registered Idempotency-Key header is supported by the baseline HTTP adapter.");
            request.Headers.TryAddWithoutValidation(header, snapshot.IdempotencyKey);
        }

        if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
            request.Content = JsonContent.Create(snapshot.Arguments);

        try
        {
            using var response = await clients.CreateClient("synentra-governed")
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            string? payload = null;
            if (response.Content is not null)
            {
                var bytes = await BoundedResponse.ReadAsync(response.Content, options.Value.MaxResponseBodySizeBytes ?? (2 * 1024 * 1024), ct);
                payload = bytes.Length == 0 ? null : Encoding.UTF8.GetString(bytes);
            }

            var upstreamReference = response.Headers.Location?.ToString();
            var status = response.IsSuccessStatusCode ? ReceiptStatus.Responded : ReceiptStatus.ToolError;
            return new(status, upstreamReference, payload, (int)response.StatusCode, timestamp,
                response.IsSuccessStatusCode ? null : $"HTTP_{(int)response.StatusCode}");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new(ReceiptStatus.Unknown, null, null, null, timestamp, "DISPATCH_CANCELLED_OR_RESPONSE_LOST");
        }
        catch (OperationCanceledException)
        {
            return new(ReceiptStatus.Unknown, null, null, null, timestamp, "HTTP_TIMEOUT_AFTER_POSSIBLE_DISPATCH");
        }
        catch (InvalidDataException)
        {
            return new(ReceiptStatus.Unknown, null, null, null, timestamp, "UPSTREAM_RESPONSE_TOO_LARGE");
        }
        catch (SecurityException)
        {
            return new(ReceiptStatus.ConnectionFailed, null, null, null, timestamp, "DESTINATION_REJECTED");
        }
        catch (HttpRequestException ex) when (ex.HttpRequestError is HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError)
        {
            return new(ReceiptStatus.ConnectionFailed, null, null, null, timestamp, "UPSTREAM_CONNECTION_FAILED");
        }
        catch (HttpRequestException)
        {
            return new(ReceiptStatus.Unknown, null, null, null, timestamp, "TRANSPORT_UNCERTAIN");
        }
    }
}
