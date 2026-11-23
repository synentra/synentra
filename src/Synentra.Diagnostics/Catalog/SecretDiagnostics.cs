namespace Synentra.Diagnostics.Catalog;

public static class SecretDiagnostics
{
    /// <summary>SYNENTRA-SEC-0001</summary>
    public static readonly Diagnostic SecretRefEmpty = Diagnostic.Define(
        module: DiagnosticModules.Secrets,
        category: DiagnosticCategory.Validation,
        sequence: 1,
        message: "Secret reference is empty.");

    /// <summary>SYNENTRA-SEC-0002</summary>
    public static readonly Diagnostic SecretRefAmbiguous = Diagnostic.Define(
        module: DiagnosticModules.Secrets,
        category: DiagnosticCategory.Validation,
        sequence: 2,
        message: "Secret reference is ambiguous (looks like a Windows path).");

    /// <summary>SYNENTRA-SEC-0003</summary>
    public static readonly Diagnostic BindingIdMismatch = Diagnostic.Define(
        module: DiagnosticModules.Secrets,
        category: DiagnosticCategory.Conflict,
        sequence: 3,
        message: "The provided binding ID does not match the requested ID.");

    /// <summary>SYNENTRA-SEC-0004</summary>
    public static readonly Diagnostic BindingNotFound = Diagnostic.Define(
        module: DiagnosticModules.Secrets,
        category: DiagnosticCategory.NotFound,
        sequence: 4,
        message: "The requested binding was not found.");

    /// <summary>SYNENTRA-SEC-0005</summary>
    public static readonly Diagnostic InvalidRequest = Diagnostic.Define(
        module: DiagnosticModules.Secrets,
        category: DiagnosticCategory.Validation,
        sequence: 5,
        message: "The provided request is invalid.");

    /// <summary>SYNENTRA-SEC-0006</summary>
    public static readonly Diagnostic InvalidCredentialProvider = Diagnostic.Define(
        module: DiagnosticModules.Secrets,
        category: DiagnosticCategory.Validation,
        sequence: 6,
        message: "The specified credential provider is not valid.");

    /// <summary>SYNENTRA-SEC-0007</summary>
    public static readonly Diagnostic InvalidSecretReference = Diagnostic.Define(
        module: DiagnosticModules.Secrets,
        category: DiagnosticCategory.Validation,
        sequence: 7,
        message: "The specified secret reference is not valid.");

    /// <summary>SYNENTRA-SEC-0008</summary>
    public static readonly Diagnostic CredentialUnavailable = Diagnostic.Define(
        module: DiagnosticModules.Secrets,
        category: DiagnosticCategory.NotFound,
        sequence: 8,
        message: "A required credential is unavailable.");
}