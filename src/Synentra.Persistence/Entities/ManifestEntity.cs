namespace Synentra.Persistence.Entities;

public sealed class ManifestEntity
{
    public string Id { get; set; } = "";
    public string ManifestId { get; set; } = "";
    public string Revision { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public string Bundle { get; set; } = "";
    public long StoredAt { get; set; }
}
