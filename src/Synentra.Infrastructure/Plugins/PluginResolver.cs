using Synentra.Application.Common.Plugins;

namespace Synentra.Infrastructure.Plugins;

public sealed class PluginResolver(PluginCatalog catalog) : IPluginResolver
{
    public bool HasRegistration(string? id) => id is not null && catalog.All.Any(x => x.Id == id);

    public IPluginRegistrationInfo? Find(string id, string version)
    {
        var p = catalog.All.SingleOrDefault(x => x.Id == id && x.Version == version);
        return p is null ? null : new(p.Id, p.Version, p.ConfigurationJsonSchema);
    }
}
