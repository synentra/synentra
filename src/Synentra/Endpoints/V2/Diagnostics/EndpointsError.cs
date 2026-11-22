using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Endpoints.V2.Diagnostics;

/// <summary>
/// Endpoints-layer diagnostics — transport and request-shape failures.
/// Owns the 3001–3999 range of each module.
/// Higher-layer conditions (not-found, permission, conflict) are owned by
/// Application and referenced here, never re-declared.
/// </summary>
public static class EndpointsError
{
    public static class Authentication
    {
        /// <summary>SYNENTRA-AUTH-3001</summary>
        public static readonly Diagnostic MissingAgentCredential = Diagnostic.Define(
            module: DiagnosticModules.Authentication,
            category: DiagnosticCategory.Authentication,
            sequence: 3001,
            message: "Missing agent credential.");

        /// <summary>SYNENTRA-AUTH-3002</summary>
        public static readonly Diagnostic CredentialValidationFailed = Diagnostic.Define(
            module: DiagnosticModules.Authentication,
            category: DiagnosticCategory.Authentication,
            sequence: 3002,
            message: "Credential validation failed.");

        /// <summary>SYNENTRA-AUTH-3003</summary>
        public static readonly Diagnostic AdministratorPermissionRequired = Diagnostic.Define(
            module: DiagnosticModules.Authentication,
            category: DiagnosticCategory.Forbidden,
            sequence: 3003,
            message: "Administrator permission required.");

        /// <summary>SYNENTRA-AUTH-3004</summary>
        public static readonly Diagnostic TrustedIssuerPermissionRequired = Diagnostic.Define(
            module: DiagnosticModules.Authentication,
            category: DiagnosticCategory.Forbidden,
            sequence: 3004,
            message: "Trusted issuer permission required.");
    }

    public static class Idempotency
    {
        /// <summary>SYNENTRA-IDM-3001</summary>
        public static readonly Diagnostic IdempotencyKeyRequired = Diagnostic.Define(
            module: DiagnosticModules.Idempotency,
            category: DiagnosticCategory.Validation,
            sequence: 3001,
            message: "Idempotency-Key is required for mutating requests.");

        /// <summary>SYNENTRA-IDM-3002</summary>
        public static readonly Diagnostic IdempotencyKeyTooLong = Diagnostic.Define(
            module: DiagnosticModules.Idempotency,
            category: DiagnosticCategory.Validation,
            sequence: 3002,
            message: "Idempotency-Key must not exceed 200 characters.");
    }

    public static class Manifest
    {
        /// <summary>SYNENTRA-MFT-3001</summary>
        public static readonly Diagnostic ManifestIdMismatch = Diagnostic.Define(
            module: DiagnosticModules.Manifest,
            category: DiagnosticCategory.Conflict,
            sequence: 3001,
            message: "The manifest identifier does not match the request.");
    }

    public static class Secrets
    {
        /// <summary>SYNENTRA-SEC-3001</summary>
        public static readonly Diagnostic BindingIdMismatch = Diagnostic.Define(
            module: DiagnosticModules.Secrets,
            category: DiagnosticCategory.Conflict,
            sequence: 3001,
            message: "The provided binding ID does not match the requested ID.");

        /// <summary>SYNENTRA-SEC-3002</summary>
        public static readonly Diagnostic BindingNotFound = Diagnostic.Define(
            module: DiagnosticModules.Secrets,
            category: DiagnosticCategory.NotFound,
            sequence: 3002,
            message: "The requested binding was not found.");

        /// <summary>SYNENTRA-SEC-3003</summary>
        public static readonly Diagnostic InvalidRequest = Diagnostic.Define(
            module: DiagnosticModules.Secrets,
            category: DiagnosticCategory.Validation,
            sequence: 3003,
            message: "The provided request is invalid.");

        /// <summary>SYNENTRA-SEC-3004</summary>
        public static readonly Diagnostic InvalidCredentialProvider = Diagnostic.Define(
            module: DiagnosticModules.Secrets,
            category: DiagnosticCategory.Validation,
            sequence: 3004,
            message: "The specified credential provider is not valid.");

        /// <summary>SYNENTRA-SEC-3005</summary>
        public static readonly Diagnostic InvalidSecretReference = Diagnostic.Define(
            module: DiagnosticModules.Secrets,
            category: DiagnosticCategory.Validation,
            sequence: 3005,
            message: "The specified secret reference is not valid.");
    }

    public static class TaskGrant
    {
        /// <summary>SYNENTRA-TGR-3001</summary>
        public static readonly Diagnostic GrantExpiryMustBeFuture = Diagnostic.Define(
            module: DiagnosticModules.TaskGrants,
            category: DiagnosticCategory.Validation,
            sequence: 3001,
            message: "Grant expiry must be in the future.");

        /// <summary>SYNENTRA-TGR-3002</summary>
        public static readonly Diagnostic GrantOperationScopeRequired = Diagnostic.Define(
            module: DiagnosticModules.TaskGrants,
            category: DiagnosticCategory.Validation,
            sequence: 3002,
            message: "At least one operation scope is required.");

        /// <summary>SYNENTRA-TGR-3003</summary>
        public static readonly Diagnostic TaskGrantNotFound = Diagnostic.Define(
            module: DiagnosticModules.TaskGrants,
            category: DiagnosticCategory.NotFound,
            sequence: 3003,
            message: "The requested grant was not found.");
    }

    public static class Execution
    {
        /// <summary>SYNENTRA-EXE-3001</summary>
        public static readonly Diagnostic ExecutionNotFound = Diagnostic.Define(
            module: DiagnosticModules.Executions,
            category: DiagnosticCategory.NotFound,
            sequence: 3001,
            message: "The requested execution was not found.");

        /// <summary>SYNENTRA-EXE-3002</summary>
        public static readonly Diagnostic VerificationNotConfigured = Diagnostic.Define(
            module: DiagnosticModules.Executions,
            category: DiagnosticCategory.Validation,
            sequence: 3002,
            message: "Execution verification is not configured.");
    }
}