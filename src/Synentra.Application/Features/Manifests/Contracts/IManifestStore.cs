namespace Synentra.Application.Features.Manifests.Contracts;

public interface IManifestStore
{
    Task<string> StoreVersionAsync(string manifestId, string version, string bundleJson, string contentHash, CancellationToken ct);
    Task<ManifestVersionSnapshot?> GetVersionAsync(string manifestId, string revision, CancellationToken ct);
    Task<ManifestVersionSnapshot?> GetVersionByVersionAsync(string manifestId, string version, CancellationToken ct);
    Task<string?> GetActiveRevisionAsync(CancellationToken ct);
    Task<string> ActivateAtomicallyAsync(string manifestId, string revision, string? expectedCurrent, CancellationToken ct);
}
