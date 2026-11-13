using Synentra.Infrastructure.Configuration.Intent;

namespace Synentra.Infrastructure.Intent.Providers.InternalBert;

public interface IModelDownloader
{
    Task EnsureModelExistsAsync(InternalOnnxConfiguration config, CancellationToken cancellationToken);
}