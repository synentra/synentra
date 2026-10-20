namespace Synentra.Application.Features.Manifests.Contracts;

public sealed record ManifestVersionSnapshot(string ManifestId, string Revision, string BundleJson);
