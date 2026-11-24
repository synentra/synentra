namespace Synentra.Diagnostics.Catalog;

public static class ExecutionDiagnostics
{
    public static Diagnostic CannotTransitionToDispatching(string state) => Define(1, $"Cannot transition to Dispatching from state {state}.", DiagnosticCategory.Conflict);
    public static readonly Diagnostic ExecutionNotFound = Define(2, "The requested execution was not found.", DiagnosticCategory.NotFound);
    public static readonly Diagnostic VerificationNotConfigured = Define(3, "Execution verification is not configured.", DiagnosticCategory.Validation);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Executions, sequence, message, category);
}