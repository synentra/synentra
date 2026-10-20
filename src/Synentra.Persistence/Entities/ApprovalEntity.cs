namespace Synentra.Persistence.Entities;

public sealed class ApprovalEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string InvocationId { get; set; } = "";
    public string SecurityScope { get; set; } = "";
    public string State { get; set; } = "";
    public long ExpiresAt { get; set; }
    public long CreatedAt { get; set; }
    public string Payload { get; set; } = "";
}
