namespace Synentra.Persistence.Entities;

public sealed class QuarantineEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string AgentId { get; set; } = "";
    public string SecurityScope { get; set; } = "";
    public string Reason { get; set; } = "";
    public long StartedAt { get; set; }
    public long? ReleasedAt { get; set; }
    public string? ReleasedBy { get; set; }
}
