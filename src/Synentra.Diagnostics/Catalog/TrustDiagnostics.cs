namespace Synentra.Diagnostics.Catalog;

public static class TrustDiagnostics
{
    /// <summary>SYNENTRA-TRS-0001</summary>
    public static readonly Diagnostic TrustScoreOutOfRange = Diagnostic.Define(
        module: DiagnosticModules.Trust,
        category: DiagnosticCategory.Validation,
        sequence: 1,
        message: "Trust score must be 0-100.");
}