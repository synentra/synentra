using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Manifests.Contracts;

namespace Synentra.Application.Features.Manifests.Commands.ActivateManifest;

public sealed record ActivateManifestCommand(
    string ManifestId,
    string TargetRevision, 
    string? ExpectedRevision) : ICommand<ActivationResult>;
