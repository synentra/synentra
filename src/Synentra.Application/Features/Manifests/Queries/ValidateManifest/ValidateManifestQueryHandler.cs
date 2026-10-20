using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Exceptions;

namespace Synentra.Application.Features.Manifests.Queries.ValidateManifest;

public sealed class ValidateManifestQueryHandler : IActionHandler<ValidateManifestQuery, ValidationResult>
{
    private readonly IBundleParser _parser;
    private readonly BundleValidator _validator;

    public ValidateManifestQueryHandler(IBundleParser parser, BundleValidator validator)
    {
        _parser = parser;
        _validator = validator;
    }

    public async Task<ValidationResult> Handle(ValidateManifestQuery request, CancellationToken cancellationToken = default)
    {
        var manifestStream = request.ManifestStream;
        var ct = cancellationToken;
        try
        {
            var bundle = _parser.Parse(manifestStream);
            return _validator.Validate(bundle);
        }
        catch (BundleParseException ex)
        {
            return new ValidationResult(false, ex.Issues.Select(i => new ValidationIssue("error", "PARSE_ERROR", i)).ToList());
        }
    }
}
