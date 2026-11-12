using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Infrastructure.Configuration.Semantic;
using Synentra.Infrastructure.Features.Intent.OpenAi;
using System.Text.Json;

namespace Synentra.Infrastructure.Features.Intent;

public sealed class OllamaIntentProvider(
    IHttpClientFactory clients, 
    IOptions<SemanticConfiguration> options, 
    ILogger<OllamaIntentProvider> logger) : IntentProviderBase
{
    public override string ProviderId => "ollama";
    public override IntentModality SupportedModalities => 
        IntentModality.Http 
        | IntentModality.Mcp 
        | IntentModality.FunctionCall 
        | IntentModality.Batch 
        | IntentModality.Stream;
    
    public override async Task<IntentLabel> ClassifyAsync(IntentEvidence evidence, CancellationToken ct)
    {
        var config = options.Value.Providers.Ollama;
        try
        {
            var endpoint = config.Endpoint.TrimEnd('/') + "/api/generate";
            using var response = await PostJsonAsync(
                clients.CreateClient("synentra-governed"),
                endpoint, 
                new {
                    model = config.Model,
                    prompt = Prompt + "\n" + BuildPrompt(evidence),
                    stream = false,
                    format = "json"
                }, 
                ct);

            if (!response.IsSuccessStatusCode)
                return Fallback();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return Parse(doc.RootElement.GetProperty("response").GetString());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) 
        {
            throw;
        }
        catch (Exception ex)
        { 
            logger.LogWarning(ex, "Ollama intent provider failed");
            return Fallback();
        }
    }
}
