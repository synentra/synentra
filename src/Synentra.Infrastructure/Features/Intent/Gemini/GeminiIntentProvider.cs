using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Infrastructure.Configuration.Intent;
using Synentra.Infrastructure.Features.Intent.OpenAi;
using System.Text.Json;

namespace Synentra.Infrastructure.Features.Intent;

public sealed class GeminiIntentProvider(
    IHttpClientFactory clients, 
    IOptions<IntentConfiguration> options, 
    ILogger<GeminiIntentProvider> logger) : IntentProviderBase
{
    public override string ProviderId => "intent-provider/gemini";
    public override IntentModality SupportedModalities => 
        IntentModality.Http
        | IntentModality.Mcp
        | IntentModality.FunctionCall
        | IntentModality.Batch
        | IntentModality.Stream;
    
    public override async Task<IntentLabel> ClassifyAsync(IntentEvidence evidence, CancellationToken ct)
    {
        var config = options.Value.Providers.Gemini;

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            return Fallback(IntentStatus.Unsupported);

        try
        {
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(config.Model)}:generateContent?key={Uri.EscapeDataString(config.ApiKey)}";

            using var response = await PostJsonAsync(clients.CreateClient("synentra-governed"), 
                endpoint,
                new { contents = new[] { new { parts = new[] { new { text = Prompt + "\n" + BuildPrompt(evidence) } } } } },
                ct);

            if (!response.IsSuccessStatusCode)
                return Fallback();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return Parse(doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Gemini intent provider failed");
            return Fallback();
        }
    }
}