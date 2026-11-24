namespace Synentra.Diagnostics.Catalog;

public static class ApprovalDiagnostics
{
    public static readonly Diagnostic ApprovalHasExpired = Define(1, "Approval has expired.", DiagnosticCategory.Conflict);
    public static Diagnostic CannotApprove(string status) => Define(2, $"Cannot approve in state {status}.", DiagnosticCategory.Conflict);
    public static Diagnostic CannotReject(string status) => Define(3, $"Cannot reject in state {status}.", DiagnosticCategory.Conflict);
    public static Diagnostic CannotRevoke(string status) => Define(4, $"Cannot revoke in state {status}.", DiagnosticCategory.Conflict);
    public static Diagnostic CannotConsume(string status) => Define(5, $"Cannot consume in state {status}.", DiagnosticCategory.Conflict);
    public static Diagnostic ApprovalNotFound(string approvalId) => Define(6, $"Approval {approvalId} not found.", DiagnosticCategory.NotFound);
    public static readonly Diagnostic ApprovalReviewerPermissionRequired = Define(7, "Reviewer permission required.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic ApprovalSelfApprovalDenied = Define(8, "Self-approval denied.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic ApprovalOnlyAdminOrRequestingAgentMayRevoke = Define(9, "Only admin or requesting agent may revoke.", DiagnosticCategory.Forbidden);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Approvals, sequence, message, category);
}