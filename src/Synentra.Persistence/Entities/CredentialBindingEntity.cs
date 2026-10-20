namespace Synentra.Persistence.Entities;

public sealed class CredentialBindingEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string TargetRef { get; set; } = "";
    public string AgentId { get; set; } = "";
    public string SecurityScope { get; set; } = "";
    public string Payload { get; set; } = "";
}
