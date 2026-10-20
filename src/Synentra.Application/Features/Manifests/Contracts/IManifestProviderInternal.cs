using Synentra.Application.Features.Manifests.Models;

namespace Synentra.Application.Features.Manifests.Contracts;

public interface IManifestProviderInternal
{
    void SetActive(GovernanceBundleModel bundle, string revision);
}
