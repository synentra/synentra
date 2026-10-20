using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Manifests.Contracts;
using System.Security.Cryptography;
using System.Text;
namespace Synentra.Application.Features.Manifests.Commands.StoreManifest;

public sealed class StoreManifestCommandHandler : IActionHandler<StoreManifestCommand, string>
{
    private readonly IBundleParser _parser;
    private readonly BundleValidator _validator;
    private readonly IManifestStore _store;

    public StoreManifestCommandHandler(IBundleParser parser, BundleValidator validator, IManifestStore store)
    {
        _parser = parser;
        _validator = validator;
        _store = store;
    }

    public async Task<string> Handle(StoreManifestCommand request, CancellationToken cancellationToken = default)
    {
        var bundleJson = request.BundleJson;
        var ct = cancellationToken;

        // Idempotency: identical version+content is a no-op, changed content conflicts
        var bundle = _parser.Parse(new MemoryStream(Encoding.UTF8.GetBytes(bundleJson)));
        var validation = _validator.Validate(bundle);
        if (!validation.IsValid)
            throw new InvalidOperationException("Cannot store invalid bundle.");

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(bundleJson)));
        var revision = await _store.StoreVersionAsync(bundle.Metadata.Name, bundle.Metadata.Version, bundleJson, hash, ct);
        return revision;
    }
}
