using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Manifests.Contracts;

namespace Synentra.Application.Features.Manifests.Queries.ValidateManifest;

public sealed record ValidateManifestQuery(Stream ManifestStream) : IQuery<ValidationResult>;
