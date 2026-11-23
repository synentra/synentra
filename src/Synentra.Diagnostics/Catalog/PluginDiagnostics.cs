namespace Synentra.Diagnostics.Catalog;

public static class PluginDiagnostics
{
    /// <summary>SYNENTRA-PLG-0001</summary>
    public static Diagnostic PluginNotFound(string pluginId, string version) => Diagnostic.Define(
        module: DiagnosticModules.Plugins,
        category: DiagnosticCategory.NotFound,
        sequence: 1,
        message: $"Plugin {pluginId} version {version} not found.");
}