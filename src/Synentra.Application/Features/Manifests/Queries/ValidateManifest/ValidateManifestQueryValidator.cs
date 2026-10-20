using FluentValidation;

namespace Synentra.Application.Features.Manifests.Queries.ValidateManifest;

public sealed class ValidateManifestQueryValidator : AbstractValidator<ValidateManifestQuery>
{
    public ValidateManifestQueryValidator()
    {
        RuleFor(x => x.ManifestStream).NotNull();
    }
}
