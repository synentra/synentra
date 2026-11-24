namespace Synentra.Diagnostics.Catalog;

public static class CredentialDiagnostics
{
    public static readonly Diagnostic CredentialNotFound = Define(1, "Credential not found.", DiagnosticCategory.NotFound);
    public static readonly Diagnostic CredentialVersionNotFound = Define(2, "Credential version not found.", DiagnosticCategory.NotFound);
    public static readonly Diagnostic CredentialProviderUnavailable = Define(3, "Credential provider unavailable.", DiagnosticCategory.Infrastructure);
    public static readonly Diagnostic CredentialUnavailableOrInvalid = Define(4, "Credential unavailable or invalid.", DiagnosticCategory.Authentication);
    public static readonly Diagnostic CredentialValueMissing = Define(5, "Credential value missing.", DiagnosticCategory.NotFound);
    public static readonly Diagnostic CredentialBootstrapConfigurationInvalid = Define(6, "Vault bootstrap configuration invalid.", DiagnosticCategory.Infrastructure);
    public static readonly Diagnostic CredentialInvalidVaultPath = Define(7, "Invalid Vault path.", DiagnosticCategory.Validation);
    public static readonly Diagnostic CredentialVersionDisabledOrNotYetValid = Define(8, "Credential version disabled or not yet valid.", DiagnosticCategory.Authentication);
    public static readonly Diagnostic CredentialBinarySecretsNotAuthenticationHeaders = Define(9, "Binary secrets are not authentication headers.", DiagnosticCategory.Validation);
    public static readonly Diagnostic CredentialExpired = Define(10, "Credential expired.", DiagnosticCategory.Authentication);
    public static readonly Diagnostic CredentialRequiresOfflineCiphertextMigration = Define(11, "Credential requires offline ciphertext migration.", DiagnosticCategory.Infrastructure);
    public static readonly Diagnostic CredentialAccessRevoked = Define(12, "Credential access revoked.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic CredentialProviderMissingOrAmbiguous = Define(13, "Credential provider missing or ambiguous.", DiagnosticCategory.Infrastructure);
    public static readonly Diagnostic CredentialInvalidatedDuringResolution = Define(14, "Credential invalidated during resolution.", DiagnosticCategory.Conflict);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Credentials, sequence, message, category);
}