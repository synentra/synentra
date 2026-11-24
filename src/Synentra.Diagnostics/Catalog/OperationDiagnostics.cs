namespace Synentra.Diagnostics.Catalog;

public static class OperationDiagnostics
{
    public static readonly Diagnostic InvalidOperationId = Define(1, "OperationId cannot be empty.", DiagnosticCategory.Validation);
    public static readonly Diagnostic OperationIdTooLong = Define(2, "OperationId cannot exceed 256 characters.", DiagnosticCategory.Validation);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Operation, sequence, message, category);
}