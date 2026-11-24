namespace Synentra.Diagnostics.Catalog;

public static class RiskDiagnostics
{
    public static readonly Diagnostic RiskScoreOutOfRange = Define(1, "Risk score must be 0-1.", DiagnosticCategory.Validation);
    public static readonly Diagnostic CoverageOutOfRange = Define(2, "Coverage must be 0-1.", DiagnosticCategory.Validation);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Risk, sequence, message, category);
}