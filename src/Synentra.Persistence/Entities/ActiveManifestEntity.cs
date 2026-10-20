namespace Synentra.Persistence.Entities;

public sealed class ActiveManifestEntity
{
    public int Id { get; set; }
    public long Version { get; set; }
    public string? ManifestVersionId { get; set; }
    public string? Revision { get; set; }
}
