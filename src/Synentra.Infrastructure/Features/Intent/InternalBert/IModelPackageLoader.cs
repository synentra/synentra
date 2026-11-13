using Synentra.Infrastructure.Configuration.Intent;

namespace Synentra.Infrastructure.Intent.Providers.InternalBert;

public interface IModelPackageLoader
{
    Task<ModelAssets> LoadAsync(InternalOnnxConfiguration config, CancellationToken cancellationToken);
}