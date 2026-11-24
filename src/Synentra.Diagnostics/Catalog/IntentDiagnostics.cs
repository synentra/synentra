namespace Synentra.Diagnostics.Catalog;

public static class IntentDiagnostics
{
    public static readonly Diagnostic ConfidenceOutOfRange = Define(1,"Confidence must be 0-1.", DiagnosticCategory.Validation);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Intent, sequence, message, category);
}