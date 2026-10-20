using Synentra.Application.Common.Dispatching;

namespace Synentra.Application.Features.Manifests.Commands.StoreManifest;

public sealed record StoreManifestCommand(string BundleJson) : ICommand<string>;
