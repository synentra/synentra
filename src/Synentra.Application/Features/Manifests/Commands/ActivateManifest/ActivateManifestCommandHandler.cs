using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Exceptions;
using System.Text;

namespace Synentra.Application.Features.Manifests.Commands.ActivateManifest;

public sealed class ActivateManifestCommandHandler : IActionHandler<ActivateManifestCommand, ActivationResult>
{
    private readonly IBundleParser _parser;
    private readonly BundleValidator _validator;
    private readonly IManifestStore _store;
    private readonly IManifestProviderInternal _provider;
    public ActivateManifestCommandHandler(IBundleParser parser, BundleValidator validator, IManifestStore store, IManifestProviderInternal provider)
    {
        _parser = parser;
        _validator = validator;
        _store = store;
        _provider = provider;
    }

    public async Task<ActivationResult> Handle(ActivateManifestCommand request, CancellationToken cancellationToken = default)
    {
        var manifestId = request.ManifestId;
        var expectedRevision = request.ExpectedRevision;
        var ct = cancellationToken;
        var currentRevision = await _store.GetActiveRevisionAsync(ct);

        if (expectedRevision != currentRevision)
            throw new ActivationConflictException(currentRevision ?? "", expectedRevision);

        var version = await _store.GetVersionByVersionAsync(manifestId, request.TargetRevision, ct)
            ?? await _store.GetVersionAsync(manifestId, request.TargetRevision, ct)
            ?? throw new InvalidOperationException($"Manifest '{manifestId}' version '{request.TargetRevision}' not found.");

        var bundle = _parser.Parse(new MemoryStream(Encoding.UTF8.GetBytes(version.BundleJson)));
        var validation = _validator.Validate(bundle);
        if (!validation.IsValid)
            return new ActivationResult(false, currentRevision, version.Revision, validation.Issues);

        var newRevision = await _store.ActivateAtomicallyAsync(manifestId, version.Revision, currentRevision, ct);
        _provider.SetActive(bundle, newRevision);
        return new ActivationResult(true, currentRevision, newRevision, Array.Empty<ValidationIssue>());
    }
}
