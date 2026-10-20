namespace Synentra.Persistence.Entities;

public sealed class IdempotencyEntity
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string ScopeKey { get; set; } = "";
    public string SecurityScope { get; set; } = "";
    public string ActorId { get; set; } = "";
    public string OperationId { get; set; } = "";
    public string Key { get; set; } = "";
    public string CanonicalInputDigest { get; set; } = "";
    public string ExactBindingDigest { get; set; } = "";
    public string InvocationId { get; set; } = "";
    public long ExpiresAt { get; set; }
}
