namespace Synentra.Diagnostics.Catalog;

public static class PersistenceDiagnostics
{
    public static readonly Diagnostic CredentialWriteConflict = Define(1, "Concurrent or duplicate write rejected.", DiagnosticCategory.Conflict);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Persistence, sequence, message, category);
}