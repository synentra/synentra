using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Manifests.Contracts;

namespace Synentra.Application.Features.Manifests.Queries.GetActiveManifestRevision;

public sealed class GetActiveManifestRevisionQueryHandler : IActionHandler<GetActiveManifestRevisionQuery, string?>
{
    private readonly IManifestStore _store;

    public GetActiveManifestRevisionQueryHandler(IManifestStore store)
    {
        _store = store;
    }

    public async Task<string?> Handle(GetActiveManifestRevisionQuery request, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        return await _store.GetActiveRevisionAsync(ct);
    }
}
