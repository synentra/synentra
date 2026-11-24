namespace Synentra.Diagnostics.Catalog;

public static class PluginDiagnostics
{
    public static Diagnostic PluginNotFound(string pluginId, string version) => Define(1, $"Plugin {pluginId} version {version} not found.", DiagnosticCategory.NotFound);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Plugins, sequence, message, category);
}