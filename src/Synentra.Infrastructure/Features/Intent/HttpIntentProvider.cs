using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Synentra.Application.Features.Intent.Contracts;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Infrastructure.Configuration;
using Synentra.Infrastructure.Networking;

namespace Synentra.Infrastructure.Features.Intent;

public sealed class HttpIntentProvider(IHttpClientFactory clients, IOptions<InfrastructureOptions> options) : IIntentProvider
{
    public string ProviderId => options.Value.IntentProviderId;
    public string ContractVersion => "1.0.0";
    public IntentModality SupportedModalities => (IntentModality)(-1);

    public async Task<IntentLabel> ClassifyAsync(IntentEvidence evidence, CancellationToken ct)
    {
        if (options.Value.IntentEndpoint is null)
            return new("unavailable", 0, IntentStatus.Error, ProviderId);

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Value.IntentEndpoint)
        {
            Content = JsonContent.Create(evidence)
        };

        using var response = await clients.CreateClient("synentra-governed")
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();
        var result = JsonSerializer.Deserialize<IntentLabel>(await BoundedResponse.ReadAsync(response.Content, 65536, ct), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return result is not null && double.IsFinite(result.Confidence) ? result : new("invalid", 0, IntentStatus.Error, ProviderId);
    }
}
