using FluentValidation;

namespace Synentra.Application.Features.Manifests.Commands.StoreManifest;

public sealed class StoreManifestCommandValidator : AbstractValidator<StoreManifestCommand>
{
    public StoreManifestCommandValidator()
    {
        RuleFor(x => x.BundleJson).NotEmpty();
    }
}
