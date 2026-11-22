using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Application.Common.Diagnostics;

/// <summary>
/// Application-layer diagnostics — workflow, repository-empty, and policy outcomes.
/// Owns the 1001–1999 range of each module.
/// </summary>
public static class ApplicationError
{
    public static class Approval
    {
        /// <summary>SYNENTRA-APR-1001</summary>
        public static Diagnostic ApprovalNotFound(string approvalId) => Diagnostic.Define(
            module: DiagnosticModules.Approvals,
            category: DiagnosticCategory.NotFound,
            sequence: 1001,
            message: $"Approval {approvalId} not found.");

        /// <summary>SYNENTRA-APR-1002</summary>
        public static readonly Diagnostic ApprovalReviewerPermissionRequired = Diagnostic.Define(
            module: DiagnosticModules.Approvals,
            category: DiagnosticCategory.Forbidden,
            sequence: 1002,
            message: "Reviewer permission required.");

        /// <summary>SYNENTRA-APR-1003</summary>
        public static readonly Diagnostic ApprovalSelfApprovalDenied = Diagnostic.Define(
            module: DiagnosticModules.Approvals,
            category: DiagnosticCategory.Forbidden,
            sequence: 1003,
            message: "Self-approval denied.");

        /// <summary>SYNENTRA-APR-1004</summary>
        public static readonly Diagnostic ApprovalOnlyAdminOrRequestingAgentMayRevoke = Diagnostic.Define(
            module: DiagnosticModules.Approvals,
            category: DiagnosticCategory.Forbidden,
            sequence: 1004,
            message: "Only admin or requesting agent may revoke.");
    }

    public static class Invocation
    {
        /// <summary>SYNENTRA-INV-1001</summary>
        public static Diagnostic InvocationNotFound(string invocationId) => Diagnostic.Define(
            module: DiagnosticModules.Invocations,
            category: DiagnosticCategory.NotFound,
            sequence: 1001,
            message: $"Invocation {invocationId} not found.");

        /// <summary>SYNENTRA-INV-1002</summary>
        public static readonly Diagnostic CrossAgentResumeDenied = Diagnostic.Define(
            module: DiagnosticModules.Invocations,
            category: DiagnosticCategory.Forbidden,
            sequence: 1002,
            message: "Cross-agent resume denied.");

        /// <summary>SYNENTRA-INV-1003</summary>
        public static readonly Diagnostic NoApprovalBoundToInvocation = Diagnostic.Define(
            module: DiagnosticModules.Invocations,
            category: DiagnosticCategory.Conflict,
            sequence: 1003,
            message: "No approval bound to invocation.");

        /// <summary>SYNENTRA-INV-1004</summary>
        public static Diagnostic ApprovalNotInApprovedState(string status) => Diagnostic.Define(
            module: DiagnosticModules.Invocations,
            category: DiagnosticCategory.Conflict,
            sequence: 1004,
            message: $"Approval not in approved state: {status}.");
    }

    public static class TaskGrant
    {
        /// <summary>SYNENTRA-TGR-1001</summary>
        public static readonly Diagnostic IssuerCannotGrantZeroOrNegativeAuthority = Diagnostic.Define(
            module: DiagnosticModules.TaskGrants,
            category: DiagnosticCategory.Validation,
            sequence: 1001,
            message: "Issuer cannot grant zero or negative authority.");

        /// <summary>SYNENTRA-TGR-1002</summary>
        public static Diagnostic GrantNotFound(string grantId) => Diagnostic.Define(
            module: DiagnosticModules.TaskGrants,
            category: DiagnosticCategory.NotFound,
            sequence: 1002,
            message: $"Grant {grantId} not found.");

        /// <summary>SYNENTRA-TGR-1003</summary>
        public static readonly Diagnostic OnlyIssuerOrAdminMayRevoke = Diagnostic.Define(
            module: DiagnosticModules.TaskGrants,
            category: DiagnosticCategory.Forbidden,
            sequence: 1003,
            message: "Only issuer or admin may revoke grant.");
    }

    public static class Plugin
    {
        /// <summary>SYNENTRA-PLG-1001</summary>
        public static Diagnostic PluginNotFound(string pluginId, string version) => Diagnostic.Define(
            module: DiagnosticModules.Plugins,
            category: DiagnosticCategory.NotFound,
            sequence: 1001,
            message: $"Plugin {pluginId} version {version} not found.");
    }
}