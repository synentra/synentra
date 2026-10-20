namespace Synentra.Persistence.Entities;

public sealed class TrustEvidenceEntity
{
    public string Id { get; set; } = "";
    public string AgentId { get; set; } = "";
    public string SecurityScope { get; set; } = "";
    public string EvidenceId { get; set; } = "";
    public string Type { get; set; } = "";
    public long ObservedAt { get; set; }
    public string Payload { get; set; } = "";
}
