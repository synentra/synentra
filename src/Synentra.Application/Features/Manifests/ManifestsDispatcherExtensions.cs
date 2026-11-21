using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Manifests.Commands.ActivateManifest;
using Synentra.Application.Features.Manifests.Commands.StoreManifest;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Queries.ValidateManifest;

namespace Synentra.Application.Features.Manifests;

public static class ManifestsDispatcherExtensions
{
    public static async Task<ValidationResult> ValidateManifest(
        this IDispatcher dispatcher,
        Stream manifestStream,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new ValidateManifestQuery(manifestStream), cancellationToken);
    }

    public static async Task<string> StoreManifest(
        this IDispatcher dispatcher,
        string bundleJson,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new StoreManifestCommand(bundleJson), cancellationToken);
    }

    public static async Task<ActivationResult> Activate(
        this IDispatcher dispatcher,
        string manifestId,
        string targetRevision,
        string? expectedRevision,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new ActivateManifestCommand(manifestId, targetRevision, expectedRevision), cancellationToken);
    }
}
