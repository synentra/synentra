namespace Synentra.Persistence.Entities;

public sealed class TrustEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string AgentId { get; set; } = "";
    public string SecurityScope { get; set; } = "";
    public double Score { get; set; }
    public string State { get; set; } = "";
    public long UpdatedAt { get; set; }
}
