namespace Synentra.Diagnostics.Catalog;

public static class QuarantineDiagnostics
{
    public static readonly Diagnostic AlreadyReleased = Define(1, "Quarantine already released.", DiagnosticCategory.Conflict);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Quarantine, sequence, message, category);
}