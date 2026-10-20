namespace Synentra.Persistence.Entities;

public sealed class InvocationEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string SecurityScope { get; set; } = "";
    public string Digest { get; set; } = "";
    public string Payload { get; set; } = "";
}
