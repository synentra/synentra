namespace Synentra.Diagnostics.Catalog;

public static class IdempotencyDiagnostics
{
    public static readonly Diagnostic IdempotencyKeyRequired = Define(1, "Idempotency-Key is required for mutating requests.");
    public static readonly Diagnostic IdempotencyKeyTooLong = Define(2, "Idempotency-Key must not exceed 200 characters.");

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Idempotency, sequence, message, category);
}