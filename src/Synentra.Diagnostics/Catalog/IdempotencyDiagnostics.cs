namespace Synentra.Diagnostics.Catalog;

public static class IdempotencyDiagnostics
{
    /// <summary>SYNENTRA-IDM-0001</summary>
    public static readonly Diagnostic IdempotencyKeyRequired = Diagnostic.Define(
        module: DiagnosticModules.Idempotency,
        category: DiagnosticCategory.Validation,
        sequence: 1,
        message: "Idempotency-Key is required for mutating requests.");

    /// <summary>SYNENTRA-IDM-0002</summary>
    public static readonly Diagnostic IdempotencyKeyTooLong = Diagnostic.Define(
        module: DiagnosticModules.Idempotency,
        category: DiagnosticCategory.Validation,
        sequence: 2,
        message: "Idempotency-Key must not exceed 200 characters.");
}