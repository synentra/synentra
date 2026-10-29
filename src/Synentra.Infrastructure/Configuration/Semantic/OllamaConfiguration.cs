namespace Synentra.Infrastructure.Configuration.Semantic;

public class OllamaConfiguration
{
    public bool Enabled { get; set; } = true;
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
}
