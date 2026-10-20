namespace Synentra.Application.Common.Plugins;

public sealed record IPluginRegistrationInfo(
    string Id, 
    string Version, 
    string ConfigurationJsonSchema);
