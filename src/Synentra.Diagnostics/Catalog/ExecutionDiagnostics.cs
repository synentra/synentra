namespace Synentra.Diagnostics.Catalog;

public static class ExecutionDiagnostics
{
    /// <summary>SYNENTRA-EXE-0001</summary>
    public static Diagnostic CannotTransitionToDispatching(string state) => Diagnostic.Define(
        module: DiagnosticModules.Executions,
        category: DiagnosticCategory.Conflict,
        sequence: 1,
        message: $"Cannot transition to Dispatching from state {state}.");

    /// <summary>SYNENTRA-EXE-0002</summary>
    public static readonly Diagnostic ExecutionNotFound = Diagnostic.Define(
        module: DiagnosticModules.Executions,
        category: DiagnosticCategory.NotFound,
        sequence: 2,
        message: "The requested execution was not found.");

    /// <summary>SYNENTRA-EXE-0003</summary>
    public static readonly Diagnostic VerificationNotConfigured = Diagnostic.Define(
        module: DiagnosticModules.Executions,
        category: DiagnosticCategory.Validation,
        sequence: 3,
        message: "Execution verification is not configured.");
}