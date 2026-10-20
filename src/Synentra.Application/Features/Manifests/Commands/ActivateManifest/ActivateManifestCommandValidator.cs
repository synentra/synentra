using FluentValidation;

namespace Synentra.Application.Features.Manifests.Commands.ActivateManifest;

public sealed class ActivateManifestCommandValidator : AbstractValidator<ActivateManifestCommand>
{
    public ActivateManifestCommandValidator()
    {
        RuleFor(x => x.ManifestId).NotEmpty();
        RuleFor(x => x.TargetRevision).NotEmpty();
    }
}
