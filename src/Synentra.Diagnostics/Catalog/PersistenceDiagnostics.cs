namespace Synentra.Diagnostics.Catalog;

public static class PersistenceDiagnostics
{
    /// <summary>SYNENTRA-PER-0001</summary>
    public static readonly Diagnostic CredentialWriteConflict = Diagnostic.Define(
        module: DiagnosticModules.Persistence,
        category: DiagnosticCategory.Conflict,
        sequence: 1,
        message: "Concurrent or duplicate write rejected.");
}