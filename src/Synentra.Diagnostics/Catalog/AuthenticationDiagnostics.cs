namespace Synentra.Diagnostics.Catalog;

public static class AuthenticationDiagnostics
{
    /// <summary>SYNENTRA-AUTH-0001</summary>
    public static readonly Diagnostic MissingAgentCredential = Diagnostic.Define(
        module: DiagnosticModules.Authentication,
        category: DiagnosticCategory.Authentication,
        sequence: 1,
        message: "Missing agent credential.");

    /// <summary>SYNENTRA-AUTH-0002</summary>
    public static readonly Diagnostic CredentialValidationFailed = Diagnostic.Define(
        module: DiagnosticModules.Authentication,
        category: DiagnosticCategory.Authentication,
        sequence: 2,
        message: "Credential validation failed.");

    /// <summary>SYNENTRA-AUTH-0003</summary>
    public static readonly Diagnostic AdministratorPermissionRequired = Diagnostic.Define(
        module: DiagnosticModules.Authentication,
        category: DiagnosticCategory.Forbidden,
        sequence: 3,
        message: "Administrator permission required.");

    /// <summary>SYNENTRA-AUTH-0004</summary>
    public static readonly Diagnostic TrustedIssuerPermissionRequired = Diagnostic.Define(
        module: DiagnosticModules.Authentication,
        category: DiagnosticCategory.Forbidden,
        sequence: 4,
        message: "Trusted issuer permission required.");

    /// <summary>SYNENTRA-AUTH-0005</summary>
    public static readonly Diagnostic UnauthorizedAccess = Diagnostic.Define(
        module: DiagnosticModules.Authentication,
        category: DiagnosticCategory.Authentication,
        sequence: 5,
        message: "The authenticated principal is not authorized for this operation.");
}