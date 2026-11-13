using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Infrastructure.Configuration.Intent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Synentra.Infrastructure.Features.Intent.AzureAi;

public sealed class AzureAiIntentProvider(
    IHttpClientFactory clients, 
    IOptions<IntentConfiguration> options, 
    ILogger<AzureAiIntentProvider> logger) : IntentProviderBase
{
    public override string ProviderId => "intent-provider/azure";

    public override IntentModality SupportedModalities =>
        IntentModality.Http
        | IntentModality.Mcp
        | IntentModality.FunctionCall
        | IntentModality.Batch
        | IntentModality.Stream;

    public override async Task<IntentLabel> ClassifyAsync(IntentEvidence evidence, CancellationToken ct)
    {
        var config = options.Value.Providers.AzureAi;

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            return Fallback(IntentStatus.Unsupported);

        try
        {
            var endpoint = config.Endpoint.TrimEnd('/') + "/openai/deployments/" + Uri.EscapeDataString(config.Model) + "/chat/completions?api-version=2024-10-21";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            var payload = new
            {
                config.Model,
                temperature = 0,
                messages = new[]
                {
                    new { role = "system", content = Prompt },
                    new { role = "user", content = BuildPrompt(evidence) }
                }
            };
            request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            using var response = await clients.CreateClient("synentra-governed").SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
                return Fallback();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return Parse(content);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Intent provider {ProviderId} failed", ProviderId);
            return Fallback();
        }
    }
}