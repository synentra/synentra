using Synentra.Application.Features.Manifests.Models;

namespace Synentra.Application.Features.Manifests.Contracts;

public interface IManifestProvider
{
    GovernanceBundleModel? ActiveBundle { get; }
    string ActiveRevision { get; }
}
