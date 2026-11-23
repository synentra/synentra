namespace Synentra.Diagnostics.Catalog;

public static class CredentialDiagnostics
{
    /// <summary>SYNENTRA-CRD-0001</summary>
    public static readonly Diagnostic CredentialNotFound = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.NotFound,
        sequence: 1,
        message: "Credential not found.");

    /// <summary>SYNENTRA-CRD-0002</summary>
    public static readonly Diagnostic CredentialVersionNotFound = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.NotFound,
        sequence: 2,
        message: "Credential version not found.");

    /// <summary>SYNENTRA-CRD-0003</summary>
    public static readonly Diagnostic CredentialProviderUnavailable = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.Infrastructure,
        sequence: 3,
        message: "Credential provider unavailable.");

    /// <summary>SYNENTRA-CRD-0004</summary>
    public static readonly Diagnostic CredentialUnavailableOrInvalid = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.Authentication,
        sequence: 4,
        message: "Credential unavailable or invalid.");

    /// <summary>SYNENTRA-CRD-0005</summary>
    public static readonly Diagnostic CredentialValueMissing = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.NotFound,
        sequence: 5,
        message: "Credential value missing.");

    /// <summary>SYNENTRA-CRD-0006</summary>
    public static readonly Diagnostic CredentialBootstrapConfigurationInvalid = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.Infrastructure,
        sequence: 6,
        message: "Vault bootstrap configuration invalid.");

    /// <summary>SYNENTRA-CRD-0007</summary>
    public static readonly Diagnostic CredentialInvalidVaultPath = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.Validation,
        sequence: 7,
        message: "Invalid Vault path.");

    /// <summary>SYNENTRA-CRD-0008</summary>
    public static readonly Diagnostic CredentialVersionDisabledOrNotYetValid = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.Authentication,
        sequence: 8,
        message: "Credential version disabled or not yet valid.");

    /// <summary>SYNENTRA-CRD-0009</summary>
    public static readonly Diagnostic CredentialBinarySecretsNotAuthenticationHeaders = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.Validation,
        sequence: 9,
        message: "Binary secrets are not authentication headers.");

    /// <summary>SYNENTRA-CRD-0010</summary>
    public static readonly Diagnostic CredentialExpired = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.Authentication,
        sequence: 10,
        message: "Credential expired.");

    /// <summary>SYNENTRA-CRD-0011</summary>
    public static readonly Diagnostic CredentialRequiresOfflineCiphertextMigration = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.Infrastructure,
        sequence: 11,
        message: "Credential requires offline ciphertext migration.");

    /// <summary>SYNENTRA-CRD-0012</summary>
    public static readonly Diagnostic CredentialAccessRevoked = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.Forbidden,
        sequence: 12,
        message: "Credential access revoked.");

    /// <summary>SYNENTRA-CRD-0013</summary>
    public static readonly Diagnostic CredentialProviderMissingOrAmbiguous = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.Infrastructure,
        sequence: 13,
        message: "Credential provider missing or ambiguous.");

    /// <summary>SYNENTRA-CRD-0014</summary>
    public static readonly Diagnostic CredentialInvalidatedDuringResolution = Diagnostic.Define(
        module: DiagnosticModules.Credentials,
        category: DiagnosticCategory.Conflict,
        sequence: 14,
        message: "Credential invalidated during resolution.");
}