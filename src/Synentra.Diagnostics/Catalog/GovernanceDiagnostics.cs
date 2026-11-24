namespace Synentra.Diagnostics.Catalog;

public static class GovernanceDiagnostics
{
    public static readonly Diagnostic GovernanceDependencyUnavailable = Define(1, "A required governance dependency is unavailable.", DiagnosticCategory.NotFound);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Governance, sequence, message, category);
}