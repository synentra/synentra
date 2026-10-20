namespace Synentra.Persistence.Entities;

public sealed class ReservationEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string BudgetId { get; set; } = "";
    public string ExecutionId { get; set; } = "";
    public long Reserved { get; set; }
    public long Settled { get; set; }
    public long Released { get; set; }
    public bool SettlementApplied { get; set; }
    public bool ReleaseApplied { get; set; }
    public string? EvidenceRef { get; set; }
}
