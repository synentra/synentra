namespace Synentra.Persistence.Entities;

public sealed class SecretEntity
{
    public string Id { get; set; } = "";
    public string Path { get; set; } = "";
    public string SecretVersion { get; set; } = "";
    public string Type { get; set; } = "";
    public string Ciphertext { get; set; } = "";
    public long? ExpiresAt { get; set; }
}
