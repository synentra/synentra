namespace Synentra.Diagnostics.Catalog;

public static class InfrastructureDiagnostics
{
    public static readonly Diagnostic InternalError = Define(1, "An unexpected error occurred.", DiagnosticCategory.Infrastructure);
    public static readonly Diagnostic InvalidRequest = Define(2, "Request is invalid.", DiagnosticCategory.Validation);
    public static readonly Diagnostic ConflictsRequest = Define(3, "Request conflicts with existing state.", DiagnosticCategory.Conflict);
    public static readonly Diagnostic ValidationFailed = Define(4, "Request validation failed.", DiagnosticCategory.Validation);
    public static Diagnostic KeyNotFound(string message) => Define(5, message, DiagnosticCategory.NotFound);
    public static readonly Diagnostic StartupSecretsUnavailable = Define(6, "Startup secrets are unavailable.", DiagnosticCategory.NotFound);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Infrastructure, sequence, message, category);
}