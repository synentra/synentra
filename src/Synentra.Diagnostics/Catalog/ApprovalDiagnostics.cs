namespace Synentra.Diagnostics.Catalog;

public static class ApprovalDiagnostics
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

    /// <summary>SYNENTRA-APR-0006</summary>
    public static Diagnostic ApprovalNotFound(string approvalId) => Diagnostic.Define(
        module: DiagnosticModules.Approvals,
        category: DiagnosticCategory.NotFound,
        sequence: 6,
        message: $"Approval {approvalId} not found.");

    /// <summary>SYNENTRA-APR-0007</summary>
    public static readonly Diagnostic ApprovalReviewerPermissionRequired = Diagnostic.Define(
        module: DiagnosticModules.Approvals,
        category: DiagnosticCategory.Forbidden,
        sequence: 7,
        message: "Reviewer permission required.");

    /// <summary>SYNENTRA-APR-0008</summary>
    public static readonly Diagnostic ApprovalSelfApprovalDenied = Diagnostic.Define(
        module: DiagnosticModules.Approvals,
        category: DiagnosticCategory.Forbidden,
        sequence: 8,
        message: "Self-approval denied.");

    /// <summary>SYNENTRA-APR-0009</summary>
    public static readonly Diagnostic ApprovalOnlyAdminOrRequestingAgentMayRevoke = Diagnostic.Define(
        module: DiagnosticModules.Approvals,
        category: DiagnosticCategory.Forbidden,
        sequence: 9,
        message: "Only admin or requesting agent may revoke.");
}