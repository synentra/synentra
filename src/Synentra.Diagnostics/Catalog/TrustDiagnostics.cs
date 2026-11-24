namespace Synentra.Diagnostics.Catalog;

public static class TrustDiagnostics
{
    public static readonly Diagnostic TrustScoreOutOfRange = Define(1, "Trust score must be 0-100.", DiagnosticCategory.Validation);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Trust, sequence, message, category);
}