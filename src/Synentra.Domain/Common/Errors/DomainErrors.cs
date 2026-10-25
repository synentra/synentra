using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Domain.Common.Errors;

public static class DomainErrors
{
    public static class Trust
    {
        public static readonly Diagnostic TrustScoreOutOfRange = Diagnostic.Define(
            module: DiagnosticModules.Trust,
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: "Trust score must be 0-100");
    }

    public static class Risk
    {
        public static readonly Diagnostic RiskScoreOutOfRange = Diagnostic.Define(
            module: DiagnosticModules.Risk, 
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: "Risk score must be 0-1");

        public static readonly Diagnostic CoverageOutOfRange = Diagnostic.Define(
            module: DiagnosticModules.Risk, 
            kind: DiagnosticKind.Validation, 
            sequence: 2, 
            message: "Coverage must be 0-1");
    }

    public static class Quantity
    {
        public static readonly Diagnostic NegativeQuantity = Diagnostic.Define(
            module: DiagnosticModules.Risk, 
            kind: DiagnosticKind.Validation, 
            sequence: 4, 
            message: "Quantity cannot be negative");

        public static readonly Diagnostic IncompatibleUnits = Diagnostic.Define(
            module: DiagnosticModules.Risk, 
            kind: DiagnosticKind.Validation, 
            sequence: 5, 
            message: "Cannot perform operation on quantities with incompatible units");
    }

    public static class Operation
    {
        public static readonly Diagnostic InvalidOperationId = Diagnostic.Define(
            module: DiagnosticModules.Operation, 
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: "OperationId cannot be empty");
        public static readonly Diagnostic OperationIdTooLong = Diagnostic.Define(
            module: DiagnosticModules.Operation, 
            kind: DiagnosticKind.Validation, 
            sequence: 2, 
            message: "OperationId cannot exceed 256 characters");
    }

    public static class Intent
    {
        public static readonly Diagnostic ConfidenceOutOfRange = Diagnostic.Define(
            module: DiagnosticModules.Intent, 
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: "Confidence must be 0-1");
    }

    public static class BindingDigest
    {
        public static readonly Diagnostic InvalidBindingDigest = Diagnostic.Define(
            module: DiagnosticModules.BindingDigest, 
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: "Binding digest must be a 64-character hexadecimal string");
    }

    public static class ResourceDescriptor
    {
        public static readonly Diagnostic InvalidResourceId = Diagnostic.Define(
            module: DiagnosticModules.ResourceDescriptor, 
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: "Resource type required");
        public static readonly Diagnostic InvalidResourceType = Diagnostic.Define(
            module: DiagnosticModules.ResourceDescriptor, 
            kind: DiagnosticKind.Validation, 
            sequence: 2, 
            message: "Resource type is invalid");
        public static readonly Diagnostic InvalidSecurityScope = Diagnostic.Define(
            module: DiagnosticModules.ResourceDescriptor, 
            kind: DiagnosticKind.Validation, 
            sequence: 3, 
            message: "Security scope is invalid");
        public static readonly Diagnostic InvalidAuthorityDomain = Diagnostic.Define(
            module: DiagnosticModules.ResourceDescriptor, 
            kind: DiagnosticKind.Validation, 
            sequence: 4, 
            message: "Authority domain is invalid");
    }

    public static class ToolInvocation
    {
        public static readonly Diagnostic InvalidAuthenticatedAgentId = Diagnostic.Define(
            module: DiagnosticModules.Invocations, 
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: "Authenticated agent id is required");
        public static readonly Diagnostic InvalidOperationId = Diagnostic.Define(
            module: DiagnosticModules.Invocations, 
            kind: DiagnosticKind.Validation, 
            sequence: 2, 
            message: "OperationId cannot be empty");
        public static readonly Diagnostic InvalidBindingDigest = Diagnostic.Define(
            module: DiagnosticModules.Invocations, 
            kind: DiagnosticKind.Validation, 
            sequence: 3, 
            message: "Binding digest must be a 64-character hexadecimal string");
    }

    public static class Approval
    {
        public static readonly Diagnostic ApprovalHasExpired = Diagnostic.Define(
            module: DiagnosticModules.Approvals, 
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: "Approval has expired");
        public static Diagnostic CannotApprove(string status) => Diagnostic.Define(
            module: DiagnosticModules.Approvals, 
            kind: DiagnosticKind.Validation, 
            sequence: 2, 
            message: $"Cannot approve in state {status}.");
        public static Diagnostic CannotReject(string status) => Diagnostic.Define(
            module: DiagnosticModules.Approvals, 
            kind: DiagnosticKind.Validation, 
            sequence: 3, 
            message: $"Cannot reject in state {status}.");
        public static Diagnostic CannotRevoke(string status) => Diagnostic.Define(
            module: DiagnosticModules.Approvals, 
            kind: DiagnosticKind.Validation, 
            sequence: 4, 
            message: $"Cannot revoke in state {status}.");
        public static Diagnostic CannotConsume(string status) => Diagnostic.Define(
            module: DiagnosticModules.Approvals, 
            kind: DiagnosticKind.Validation, 
            sequence: 5, 
            message: $"Cannot consume in state {status}.");
    }

    public static class ExecutionClaim
    {
        public static Diagnostic CannotTransitionToDispatching(string state) => Diagnostic.Define(
            module: DiagnosticModules.Executions, 
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: $"Cannot transition to Dispatching from state {state}.");
    }

    public static class TaskGrant
    {
        public static Diagnostic CannotRevoke(string state) => Diagnostic.Define(
            module: DiagnosticModules.TaskGrants, 
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: $"Cannot revoke in state {state}.");
    }

    public static class EffectMeasure
    {
        public static Diagnostic MeasureRequired = Diagnostic.Define(
            module: DiagnosticModules.EffectMeasure, 
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: "Measure required.");
        public static Diagnostic UnitRequired = Diagnostic.Define(
            module: DiagnosticModules.EffectMeasure, 
            kind: DiagnosticKind.Validation, 
            sequence: 2, 
            message: "Unit required.");
        public static Diagnostic ScaleMustBeNonNegative = Diagnostic.Define(
            module: DiagnosticModules.EffectMeasure, 
            kind: DiagnosticKind.Validation, 
            sequence: 3, 
            message: "Scale must be non-negative.");
        public static Diagnostic UpperBoundMustBeNonNegative = Diagnostic.Define(
            module: DiagnosticModules.EffectMeasure,
            kind: DiagnosticKind.Validation,
            sequence: 4,
            message: "Upper bound must be non-negative.");
    }

    public static class Quarantine
    {
        public static Diagnostic AlreadyReleased = Diagnostic.Define(
            module: DiagnosticModules.Quarantine, 
            kind: DiagnosticKind.Validation, 
            sequence: 1, 
            message: "Quarantine already released.");
    }

    public static class Secret
    {
        public static Diagnostic SecretRefEmpty = Diagnostic.Define(
            module: DiagnosticModules.Secrets,
            kind: DiagnosticKind.Validation,
            sequence: 1,
            message: "Secret reference is empty."
            );

        public static Diagnostic SecretRefAmbiguous = Diagnostic.Define(
            module: DiagnosticModules.Secrets,
            kind: DiagnosticKind.Validation,
            sequence: 2,
            message: "Secret reference is ambiguous (looks like a Windows path)."
            );
    }
}
