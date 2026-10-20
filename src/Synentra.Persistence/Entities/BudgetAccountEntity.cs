namespace Synentra.Persistence.Entities;

public sealed class BudgetAccountEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public long AuthorizedLimit { get; set; }
    public long Settled { get; set; }
    public long ActiveReserved { get; set; }
    public string KeyJson { get; set; } = "";
}
