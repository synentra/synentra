namespace Synentra.Diagnostics.Catalog;

public static class AuthenticationDiagnostics
{
    public static readonly Diagnostic MissingAgentCredential = Define(1, "Missing agent credential.", DiagnosticCategory.Authentication);
    public static readonly Diagnostic CredentialValidationFailed = Define(2, "Credential validation failed.", DiagnosticCategory.Authentication);
    public static readonly Diagnostic AdministratorPermissionRequired = Define(3, "Administrator permission required.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic TrustedIssuerPermissionRequired = Define(4, "Trusted issuer permission required.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic UnauthorizedAccess = Define(5, "The authenticated principal is not authorized for this operation.", DiagnosticCategory.Authentication);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Authentication, sequence, message, category);
}