using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Application.Common.Diagnostics;

public static class ApplicationError
{
    public static class Approval
    {
        public static Diagnostic ApprovalNotFound(string approvalId) 
            => Diagnostic.Define(
                module: DiagnosticModules.Approvals, 
                kind: DiagnosticKind.Validation, 
                sequence: 1, 
                message: $"Approval {approvalId} not found.");

        public static readonly Diagnostic ApprovalReviewerPermissionRequired 
            = Diagnostic.Define(
                module: DiagnosticModules.Approvals, 
                kind: DiagnosticKind.Validation, 
                sequence: 2, 
                message: "Reviewer permission required.");

        public static readonly Diagnostic ApprovalSelfApprovalDenied 
            = Diagnostic.Define(
                module: DiagnosticModules.Approvals, 
                kind: DiagnosticKind.Validation, 
                sequence: 3, 
                message: "Self-approval denied.");

        public static readonly Diagnostic ApprovalOnlyAdminOrRequestingAgentMayRevoke 
            = Diagnostic.Define(
                module: DiagnosticModules.Approvals, 
                kind: DiagnosticKind.Validation, 
                sequence: 4, 
                message: "Only admin or requesting agent may revoke.");
    }

    public static class Invocation
    {
        public static Diagnostic InvocationNotFound(string invocationId) 
            => Diagnostic.Define(
                module: DiagnosticModules.Invocations, 
                kind: DiagnosticKind.Validation, 
                sequence: 1, 
                message: $"Invocation {invocationId} not found.");

        public static readonly Diagnostic CrossAgentResumeDenied 
            = Diagnostic.Define(
                module: DiagnosticModules.Invocations, 
                kind: DiagnosticKind.Validation, 
                sequence: 2, 
                message: "Cross-agent resume denied.");

        public static readonly Diagnostic NoApprovalBoundToInvocation 
            = Diagnostic.Define(
                module: DiagnosticModules.Invocations, 
                kind: DiagnosticKind.Validation, 
                sequence: 3, 
                message: "No approval bound to invocation.");

        public static Diagnostic ApprovalNotInApprovedState(string status) 
            => Diagnostic.Define(
                module: DiagnosticModules.Invocations, 
                kind: DiagnosticKind.Validation, 
                sequence: 4, 
                message: $"Approval not in approved state: {status}.");
    }

    public static class TaskGrant
    {
        public static readonly Diagnostic IssuerCannotGrantZeroOrNegativeAuthority 
            = Diagnostic.Define(
                module: DiagnosticModules.TaskGrants, 
                kind: DiagnosticKind.Validation, 
                sequence: 1, 
                message: "Issuer cannot grant zero or negative authority.");

        public static Diagnostic GrantNotFound(string grantId) 
            => Diagnostic.Define(
                module: DiagnosticModules.TaskGrants, 
                kind: DiagnosticKind.Validation, 
                sequence: 2, 
                message: $"Grant {grantId} not found.");

        public static readonly Diagnostic OnlyIssuerOrAdminMayRevoke 
            = Diagnostic.Define(
                module: DiagnosticModules.TaskGrants, 
                kind: DiagnosticKind.Validation, 
                sequence: 3, 
                message: "Only issuer or admin may revoke grant.");
    }

    public static class Plugin
    {
        public static Diagnostic PluginNotFound(string pluginId, string version)
            => Diagnostic.Define(
                module: DiagnosticModules.Plugins, 
                kind: DiagnosticKind.Validation, 
                sequence: 1, 
                message: $"Plugin {pluginId} version {version} not found.");
    }
}
