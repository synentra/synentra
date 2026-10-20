namespace Synentra.Persistence.Entities;

public sealed class GrantEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string AgentId { get; set; } = "";
    public string SecurityScope { get; set; } = "";
    public string State { get; set; } = "";
    public long ExpiresAt { get; set; }
    public string Payload { get; set; } = "";
}
