namespace Synentra.Infrastructure.Configuration.Intent;

public class IntentProviders
{
    public InternalOnnxConfiguration Internal { get; set; } = new();
    public AzureAiConfiguration AzureAi { get; set; } = new();
    public OpenAiConfiguration OpenAi { get; set; } = new();
    public GeminiConfiguration Gemini { get; set; } = new();
    public OllamaConfiguration Ollama { get; set; } = new();
}