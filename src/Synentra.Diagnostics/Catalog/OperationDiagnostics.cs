namespace Synentra.Diagnostics.Catalog;

public static class OperationDiagnostics
{
    /// <summary>SYNENTRA-OPR-0001</summary>
    public static readonly Diagnostic InvalidOperationId = Diagnostic.Define(
        module: DiagnosticModules.Operation,
        category: DiagnosticCategory.Validation,
        sequence: 1,
        message: "OperationId cannot be empty.");

    /// <summary>SYNENTRA-OPR-0002</summary>
    public static readonly Diagnostic OperationIdTooLong = Diagnostic.Define(
        module: DiagnosticModules.Operation,
        category: DiagnosticCategory.Validation,
        sequence: 2,
        message: "OperationId cannot exceed 256 characters.");
}