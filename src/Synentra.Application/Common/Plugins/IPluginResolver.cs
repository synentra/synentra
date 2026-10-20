namespace Synentra.Application.Common.Plugins;

public interface IPluginResolver
{
    bool HasRegistration(string? pluginId);
    IPluginRegistrationInfo? Find(string pluginId, string version);
}
