using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Domain.Common.Errors;

/// <summary>
/// Domain-layer diagnostics — invariants and state-machine violations.
/// Owns the 0001–0999 range of each module.
/// </summary>
public static class DomainErrors
{
    public static class Trust
    {
        /// <summary>SYNENTRA-TRS-0001</summary>
        public static readonly Diagnostic TrustScoreOutOfRange = Diagnostic.Define(
            module: DiagnosticModules.Trust,
            category: DiagnosticCategory.Validation,
            sequence: 1,
            message: "Trust score must be 0-100.");
    }

    public static class Risk
    {
        /// <summary>SYNENTRA-RSK-0001</summary>
        public static readonly Diagnostic RiskScoreOutOfRange = Diagnostic.Define(
            module: DiagnosticModules.Risk,
            category: DiagnosticCategory.Validation,
            sequence: 1,
            message: "Risk score must be 0-1.");

        /// <summary>SYNENTRA-RSK-0002</summary>
        public static readonly Diagnostic CoverageOutOfRange = Diagnostic.Define(
            module: DiagnosticModules.Risk,
            category: DiagnosticCategory.Validation,
            sequence: 2,
            message: "Coverage must be 0-1.");
    }

    public static class Operation
    {
        /// <summary>SYNENTRA-OPR-0001</summary>
        public static readonly Diagnostic InvalidOperationId = Diagnostic.Define(
            module: DiagnosticModules.Operation,
            category: DiagnosticCategory.Validation,
            sequence: 1,
            message: "OperationId cannot be empty.");

        /// <summary>SYNENTRA-OPR-0002</summary>
        public static readonly Diagnostic OperationIdTooLong = Diagnostic.Define(
            module: DiagnosticModules.Operation,
            category: DiagnosticCategory.Validation,
            sequence: 2,
            message: "OperationId cannot exceed 256 characters.");
    }

    public static class Intent
    {
        /// <summary>SYNENTRA-INT-0001</summary>
        public static readonly Diagnostic ConfidenceOutOfRange = Diagnostic.Define(
            module: DiagnosticModules.Intent,
            category: DiagnosticCategory.Validation,
            sequence: 1,
            message: "Confidence must be 0-1.");
    }

    public static class BindingDigest
    {
        /// <summary>SYNENTRA-BDG-0001</summary>
        public static readonly Diagnostic InvalidBindingDigest = Diagnostic.Define(
            module: DiagnosticModules.BindingDigest,
            category: DiagnosticCategory.Validation,
            sequence: 1,
            message: "Binding digest must be a 64-character hexadecimal string.");
    }

    public static class ResourceDescriptor
    {
        /// <summary>SYNENTRA-RSD-0001</summary>
        public static readonly Diagnostic MissingResourceType = Diagnostic.Define(
            module: DiagnosticModules.ResourceDescriptor,
            category: DiagnosticCategory.Validation,
            sequence: 1,
            message: "Resource type required.");

        /// <summary>SYNENTRA-RSD-0002</summary>
        public static readonly Diagnostic InvalidResourceType = Diagnostic.Define(
            module: DiagnosticModules.ResourceDescriptor,
            category: DiagnosticCategory.Validation,
            sequence: 2,
            message: "Resource type is invalid.");

        /// <summary>SYNENTRA-RSD-0003</summary>
        public static readonly Diagnostic InvalidResourceId = Diagnostic.Define(
            module: DiagnosticModules.ResourceDescriptor,
            category: DiagnosticCategory.Validation,
            sequence: 3,
            message: "Resource id is invalid.");

        /// <summary>SYNENTRA-RSD-0004</summary>
        public static readonly Diagnostic InvalidSecurityScope = Diagnostic.Define(
            module: DiagnosticModules.ResourceDescriptor,
            category: DiagnosticCategory.Validation,
            sequence: 4,
            message: "Security scope is invalid.");

        /// <summary>SYNENTRA-RSD-0005</summary>
        public static readonly Diagnostic InvalidAuthorityDomain = Diagnostic.Define(
            module: DiagnosticModules.ResourceDescriptor,
            category: DiagnosticCategory.Validation,
            sequence: 5,
            message: "Authority domain is invalid.");
    }

    public static class Invocation
    {
        /// <summary>SYNENTRA-INV-0001</summary>
        public static readonly Diagnostic InvalidAuthenticatedAgentId = Diagnostic.Define(
            module: DiagnosticModules.Invocations,
            category: DiagnosticCategory.Validation,
            sequence: 1,
            message: "Authenticated agent id is required.");

        /// <summary>SYNENTRA-INV-0002</summary>
        public static readonly Diagnostic InvalidOperationId = Diagnostic.Define(
            module: DiagnosticModules.Invocations,
            category: DiagnosticCategory.Validation,
            sequence: 2,
            message: "OperationId cannot be empty.");

        /// <summary>SYNENTRA-INV-0003</summary>
        public static readonly Diagnostic InvalidBindingDigest = Diagnostic.Define(
            module: DiagnosticModules.Invocations,
            category: DiagnosticCategory.Validation,
            sequence: 3,
            message: "Binding digest must be a 64-character hexadecimal string.");
    }

    public static class Approval
    {
        /// <summary>SYNENTRA-APR-0001</summary>
        public static readonly Diagnostic ApprovalHasExpired = Diagnostic.Define(
            module: DiagnosticModules.Approvals,
            category: DiagnosticCategory.Conflict,
            sequence: 1,
            message: "Approval has expired.");

        /// <summary>SYNENTRA-APR-0002</summary>
        public static Diagnostic CannotApprove(string status) => Diagnostic.Define(
            module: DiagnosticModules.Approvals,
            category: DiagnosticCategory.Conflict,
            sequence: 2,
            message: $"Cannot approve in state {status}.");

        /// <summary>SYNENTRA-APR-0003</summary>
        public static Diagnostic CannotReject(string status) => Diagnostic.Define(
            module: DiagnosticModules.Approvals,
            category: DiagnosticCategory.Conflict,
            sequence: 3,
            message: $"Cannot reject in state {status}.");

        /// <summary>SYNENTRA-APR-0004</summary>
        public static Diagnostic CannotRevoke(string status) => Diagnostic.Define(
            module: DiagnosticModules.Approvals,
            category: DiagnosticCategory.Conflict,
            sequence: 4,
            message: $"Cannot revoke in state {status}.");

        /// <summary>SYNENTRA-APR-0005</summary>
        public static Diagnostic CannotConsume(string status) => Diagnostic.Define(
            module: DiagnosticModules.Approvals,
            category: DiagnosticCategory.Conflict,
            sequence: 5,
            message: $"Cannot consume in state {status}.");
    }

    public static class ExecutionClaim
    {
        /// <summary>SYNENTRA-EXE-0001</summary>
        public static Diagnostic CannotTransitionToDispatching(string state) => Diagnostic.Define(
            module: DiagnosticModules.Executions,
            category: DiagnosticCategory.Conflict,
            sequence: 1,
            message: $"Cannot transition to Dispatching from state {state}.");
    }

    public static class TaskGrant
    {
        /// <summary>SYNENTRA-TGR-0001</summary>
        public static Diagnostic CannotRevoke(string state) => Diagnostic.Define(
            module: DiagnosticModules.TaskGrants,
            category: DiagnosticCategory.Conflict,
            sequence: 1,
            message: $"Cannot revoke in state {state}.");
    }

    public static class Quarantine
    {
        /// <summary>SYNENTRA-QRT-0001</summary>
        public static readonly Diagnostic AlreadyReleased = Diagnostic.Define(
            module: DiagnosticModules.Quarantine,
            category: DiagnosticCategory.Conflict,
            sequence: 1,
            message: "Quarantine already released.");
    }

    public static class Secret
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
    }
}