using Synentra.Application.Common.Dispatching;

namespace Synentra.Application.Features.Manifests.Queries.GetActiveManifestRevision;

public sealed record GetActiveManifestRevisionQuery() : IQuery<string?>;
