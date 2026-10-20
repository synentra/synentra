namespace Synentra.Persistence.Entities;

public sealed class ExecutionClaimEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string InvocationId { get; set; } = "";
    public string ClaimedBy { get; set; } = "";
    public string State { get; set; } = "";
    public long ExpiresAt { get; set; }
}
