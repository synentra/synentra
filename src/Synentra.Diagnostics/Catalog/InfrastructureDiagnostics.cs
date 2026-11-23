namespace Synentra.Diagnostics.Catalog;

public static class InfrastructureDiagnostics
{
    /// <summary>SYNENTRA-INF-0001</summary>
    public static readonly Diagnostic InternalError = Diagnostic.Define(
        module: DiagnosticModules.Infrastructure,
        category: DiagnosticCategory.Infrastructure,
        sequence: 1,
        message: "An unexpected error occurred.");

    /// <summary>SYNENTRA-INF-0002</summary>
    public static readonly Diagnostic InvalidRequest = Diagnostic.Define(
        module: DiagnosticModules.Infrastructure,
        category: DiagnosticCategory.Validation,
        sequence: 2,
        message: "Request is invalid.");

    /// <summary>SYNENTRA-INF-0003</summary>
    public static readonly Diagnostic ConflictsRequest = Diagnostic.Define(
        module: DiagnosticModules.Infrastructure,
        category: DiagnosticCategory.Conflict,
        sequence: 3,
        message: "Request conflicts with existing state.");

    /// <summary>SYNENTRA-INF-0004</summary>
    public static readonly Diagnostic ValidationFailed = Diagnostic.Define(
        module: DiagnosticModules.Infrastructure,
        category: DiagnosticCategory.Validation,
        sequence: 4,
        message: "Request validation failed.");

    /// <summary>SYNENTRA-INF-0005</summary>
    public static Diagnostic KeyNotFound(string message) => Diagnostic.Define(
        module: DiagnosticModules.Infrastructure,
        category: DiagnosticCategory.NotFound,
        sequence: 5,
        message: message);

    /// <summary>SYNENTRA-INF-0006</summary>
    public static readonly Diagnostic StartupSecretsUnavailable = Diagnostic.Define(
        module: DiagnosticModules.Infrastructure,
        category: DiagnosticCategory.NotFound,
        sequence: 6,
        message: "Startup secrets are unavailable.");
}