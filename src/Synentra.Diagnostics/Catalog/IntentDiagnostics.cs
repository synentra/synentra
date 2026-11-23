namespace Synentra.Diagnostics.Catalog;

public static class IntentDiagnostics
{
    /// <summary>SYNENTRA-INT-0001</summary>
    public static readonly Diagnostic ConfidenceOutOfRange = Diagnostic.Define(
        module: DiagnosticModules.Intent,
        category: DiagnosticCategory.Validation,
        sequence: 1,
        message: "Confidence must be 0-1.");
}