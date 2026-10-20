using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Manifests.Commands.ActivateManifest;
using Synentra.Application.Features.Manifests.Commands.StoreManifest;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Queries.GetActiveManifestRevision;
using Synentra.Application.Features.Manifests.Queries.ValidateManifest;

namespace Synentra.Application.Features.Manifests;

public sealed class ManifestManager : IManifestManager
{
    private readonly IDispatcher _dispatcher;
    public ManifestManager(IDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task<ValidationResult> ValidateAsync(
        Stream manifestStream,
        CancellationToken ct) => _dispatcher.Dispatch(new ValidateManifestQuery(manifestStream), ct);

    public Task<string> StoreAsync(
        string bundleJson,
        CancellationToken ct) => _dispatcher.Dispatch(new StoreManifestCommand(bundleJson), ct);

    public Task<ActivationResult> ActivateAsync(
        string manifestId, 
        string targetRevision,
        string? expectedRevision, 
        CancellationToken ct) => _dispatcher.Dispatch(new ActivateManifestCommand(manifestId, targetRevision, expectedRevision), ct);

    public Task<string?> GetActiveRevisionAsync(CancellationToken ct) => _dispatcher.Dispatch(new GetActiveManifestRevisionQuery(), ct);
}
