using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Infrastructure.Diagnostics;

/// <summary>
/// Infrastructure-layer diagnostics — provider and external-system failures.
/// Owns the 2001–2999 range of each module.
/// </summary>
public static class InfrastructureErrors
{
    public static class Credential
    {
        /// <summary>SYNENTRA-CRD-2001</summary>
        public static readonly Diagnostic CredentialNotFound = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.NotFound,
            sequence: 2001,
            message: "Credential not found.");

        /// <summary>SYNENTRA-CRD-2002</summary>
        public static readonly Diagnostic CredentialVersionNotFound = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.NotFound,
            sequence: 2002,
            message: "Credential version not found.");

        /// <summary>SYNENTRA-CRD-2003</summary>
        public static readonly Diagnostic CredentialProviderUnavailable = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.Infrastructure,
            sequence: 2003,
            message: "Credential provider unavailable.");

        /// <summary>SYNENTRA-CRD-2004</summary>
        public static readonly Diagnostic CredentialUnavailableOrInvalid = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.Authentication,
            sequence: 2004,
            message: "Credential unavailable or invalid.");

        /// <summary>SYNENTRA-CRD-2005</summary>
        public static readonly Diagnostic CredentialValueMissing = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.NotFound,
            sequence: 2005,
            message: "Credential value missing.");

        /// <summary>SYNENTRA-CRD-2006</summary>
        public static readonly Diagnostic CredentialBootstrapConfigurationInvalid = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.Infrastructure,
            sequence: 2006,
            message: "Vault bootstrap configuration invalid.");

        /// <summary>SYNENTRA-CRD-2007</summary>
        public static readonly Diagnostic CredentialInvalidVaultPath = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.Validation,
            sequence: 2007,
            message: "Invalid Vault path.");

        /// <summary>SYNENTRA-CRD-2008</summary>
        public static readonly Diagnostic CredentialVersionDisabledOrNotYetValid = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.Authentication,
            sequence: 2008,
            message: "Credential version disabled or not yet valid.");

        /// <summary>SYNENTRA-CRD-2009</summary>
        public static readonly Diagnostic CredentialBinarySecretsNotAuthenticationHeaders = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.Validation,
            sequence: 2009,
            message: "Binary secrets are not authentication headers.");

        /// <summary>SYNENTRA-CRD-2010</summary>
        public static readonly Diagnostic CredentialExpired = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.Authentication,
            sequence: 2010,
            message: "Credential expired.");

        /// <summary>SYNENTRA-CRD-2011</summary>
        public static readonly Diagnostic CredentialRequiresOfflineCiphertextMigration = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.Infrastructure,
            sequence: 2011,
            message: "Credential requires offline ciphertext migration.");

        /// <summary>SYNENTRA-CRD-2012</summary>
        public static readonly Diagnostic CredentialAccessRevoked = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.Forbidden,
            sequence: 2012,
            message: "Credential access revoked.");

        /// <summary>SYNENTRA-CRD-2013</summary>
        public static readonly Diagnostic CredentialProviderMissingOrAmbiguous = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.Infrastructure,
            sequence: 2013,
            message: "Credential provider missing or ambiguous.");

        /// <summary>SYNENTRA-CRD-2014</summary>
        public static readonly Diagnostic CredentialInvalidatedDuringResolution = Diagnostic.Define(
            module: DiagnosticModules.Credentials,
            category: DiagnosticCategory.Conflict,
            sequence: 2014,
            message: "Credential invalidated during resolution.");
    }
}