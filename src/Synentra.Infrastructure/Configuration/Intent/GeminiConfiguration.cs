namespace Synentra.Infrastructure.Configuration.Intent;

public class GeminiConfiguration
{
    public bool Enabled { get; set; } = true;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.0-flash";
    public double? Temperature { get; set; } = 0.0;
    public int? MaxTokens { get; set; } = 256;
}
