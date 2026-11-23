namespace Synentra.Diagnostics.Catalog;

public static class GovernanceDiagnostics
{
    /// <summary>SYNENTRA-GOV-3001</summary>
    public static readonly Diagnostic GovernanceDependencyUnavailable = Diagnostic.Define(
        module: DiagnosticModules.Governance,
        category: DiagnosticCategory.NotFound,
        sequence: 3001,
        message: "A required governance dependency is unavailable.");
}