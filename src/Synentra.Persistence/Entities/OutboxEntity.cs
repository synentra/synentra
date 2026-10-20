namespace Synentra.Persistence.Entities;

public sealed class OutboxEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string Type { get; set; } = "";
    public string Payload { get; set; } = "";
    public long CreatedAt { get; set; }
    public long? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
