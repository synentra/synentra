using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;

namespace Synentra.Application.Common.Plugins;

public sealed class PluginCatalog
{
    private readonly Dictionary<string, IPluginRegistration> _registrations = new(StringComparer.Ordinal);
    public void Register(IPluginRegistration registration)
    {
        var key = $"{registration.Id}@{registration.Version}";
        if (_registrations.ContainsKey(key))
            throw new InvalidOperationException($"Duplicate plugin registration: {key}");
        _registrations[key] = registration;
    }

    public IPluginRegistration Resolve(string id, string version)
    {
        if (!_registrations.TryGetValue($"{id}@{version}", out var reg))
            throw new SynentraApplicationException(PluginDiagnostics.PluginNotFound(id, version));

        return reg;
    }

    public IReadOnlyCollection<IPluginRegistration> All => _registrations.Values;
}
