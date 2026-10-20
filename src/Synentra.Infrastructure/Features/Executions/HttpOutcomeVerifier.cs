using System.Text.Json;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Domain.Execution;
using Synentra.Domain.Execution.Evidence;
using Synentra.Infrastructure.Networking;

namespace Synentra.Infrastructure.Features.Executions;

public sealed class HttpOutcomeVerifier(IHttpClientFactory clients) : IOutcomeVerifier
{
    public string VerifierId => "http-readback";
    public string Version => "1.0.0";

    public async Task<OutcomeEvidence> VerifyAsync(ExecutionRecord record, VerifierRegistration registration, CancellationToken ct)
    {
        if (!registration.Configuration.TryGetValue("endpoint", out var endpoint))
            throw new InvalidOperationException("Verifier endpoint missing.");

        var separator = endpoint.Contains('?') ? "&" : "?";
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint + separator + "executionId=" + Uri.EscapeDataString(record.Id));
        using var response = await clients.CreateClient("synentra-governed").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
            return new(Guid.NewGuid().ToString("N"), record.Id, OutcomeStatus.Unknown, VerifierId, registration.Version, null, null);

        using var doc = JsonDocument.Parse(await BoundedResponse.ReadAsync(response.Content, 65536, ct));
        var e = doc.RootElement;
        if (!e.TryGetProperty("status", out var s) 
            || !Enum.TryParse<OutcomeStatus>(s.GetString(), true, out var status) 
            || status is not (OutcomeStatus.Verified or OutcomeStatus.Contradicted or OutcomeStatus.Unknown))
            status = OutcomeStatus.Unknown;

        var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(e.GetRawText())));
        return new(Guid.NewGuid().ToString("N"), record.Id, status, VerifierId, registration.Version, "Registered readback predicate evaluated.", digest);
    }
}
