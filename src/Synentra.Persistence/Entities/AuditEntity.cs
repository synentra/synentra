namespace Synentra.Persistence.Entities;

public sealed class AuditEntity
{
    public string Id { get; set; } = "";
    public string SecurityScope { get; set; } = "";
    public string ActorId { get; set; } = "";
    public string? InvocationId { get; set; }
    public string Category { get; set; } = "";
    public long Timestamp { get; set; }
    public string Payload { get; set; } = "";
}
