namespace Synentra.Diagnostics.Catalog;

public static class QuarantineDiagnostics
{
    /// <summary>SYNENTRA-QRT-0001</summary>
    public static readonly Diagnostic AlreadyReleased = Diagnostic.Define(
        module: DiagnosticModules.Quarantine,
        category: DiagnosticCategory.Conflict,
        sequence: 1,
        message: "Quarantine already released.");
}