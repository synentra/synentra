using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Infrastructure.Diagnostics;

public static class InfrastructureErrors
{
    public static class Credential
    {
        public static Diagnostic CredentialNotFound 
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 1,
                message: "Secret requires offline ciphertext migration.");

        public static Diagnostic CredentialVersionNotFound
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 2,
                message: "Secret version not found.");

        public static Diagnostic CredentialProviderUnavailable
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 3,
                message: "Credential provider unavailable.");

        public static Diagnostic CredentialUnavailableOrInvalid
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 4,
                message: "Credential unavailable or invalid.");

        public static Diagnostic CredentialValueMissing
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 5,
                message: "Secret value missing.");

        public static Diagnostic CredentialBootstrapConfigurationInvalid
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 6,
                message: "Vault bootstrap configuration invalid.");

        public static Diagnostic CredentialInvalidVaultPath
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 7,
                message: "Invalid Vault path.");

        public static Diagnostic CredentialVersionDisabledOrNotYetValid
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 8,
                message: "Secret version disabled or not yet valid.");

        public static Diagnostic CredentialBinarySecretsNotAuthenticationHeaders
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 9,
                message: "Binary secrets are not authentication headers.");

        public static Diagnostic CredentialExpired
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 10,
                message: "Secret expired.");

        public static Diagnostic CredentialRequiresOfflineCiphertextMigration
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 11,
                message: "Secret requires offline ciphertext migration.");

        public static Diagnostic CredentialAccessRevoked
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 12,
                message: "Secret access revoked.");

        public static Diagnostic CredentialProviderMissingOrAmbiguous
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 13,
                message: "Credential provider missing or ambiguous.");

        public static Diagnostic CredentialInvalidatedDuringResolution
            = Diagnostic.Define(
                module: DiagnosticModules.Credentials,
                kind: DiagnosticKind.Validation,
                sequence: 14,
                message: "Secret invalidated during resolution.");
    }
}
