namespace Synentra.Persistence.Entities;

public sealed class SecretStateEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public long Generation { get; set; }
    public bool Revoked { get; set; }
}
