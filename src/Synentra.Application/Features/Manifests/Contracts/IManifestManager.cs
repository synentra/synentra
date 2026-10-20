namespace Synentra.Application.Features.Manifests.Contracts;

public interface IManifestManager
{
    Task<ValidationResult> ValidateAsync(Stream manifestStream, CancellationToken ct);
    Task<string> StoreAsync(string bundleJson, CancellationToken ct);
    Task<ActivationResult> ActivateAsync(string manifestId, string targetRevision, string? expectedRevision, CancellationToken ct);
    Task<string?> GetActiveRevisionAsync(CancellationToken ct);
}
