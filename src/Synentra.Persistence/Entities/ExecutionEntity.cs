namespace Synentra.Persistence.Entities;

public sealed class ExecutionEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string InvocationId { get; set; } = "";
    public string State { get; set; } = "";
    public string Payload { get; set; } = "";
}
