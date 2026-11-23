namespace Synentra.Diagnostics.Catalog;

public static class RiskDiagnostics
{
    /// <summary>SYNENTRA-RSK-0001</summary>
    public static readonly Diagnostic RiskScoreOutOfRange = Diagnostic.Define(
        module: DiagnosticModules.Risk,
        category: DiagnosticCategory.Validation,
        sequence: 1,
        message: "Risk score must be 0-1.");

    /// <summary>SYNENTRA-RSK-0002</summary>
    public static readonly Diagnostic CoverageOutOfRange = Diagnostic.Define(
        module: DiagnosticModules.Risk,
        category: DiagnosticCategory.Validation,
        sequence: 2,
        message: "Coverage must be 0-1.");
}