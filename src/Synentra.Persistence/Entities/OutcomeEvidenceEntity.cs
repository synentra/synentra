namespace Synentra.Persistence.Entities;

public sealed class OutcomeEvidenceEntity
{
    public string Id { get; set; } = "";
    public string ExecutionId { get; set; } = "";
    public long ObservedAt { get; set; }
    public string Payload { get; set; } = "";
}
