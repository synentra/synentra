using Synentra.Application.Features.Manifests.Models;

namespace Synentra.Application.Features.Manifests.Contracts;

public interface IBundleParser
{
    GovernanceBundleModel Parse(Stream stream);
}
