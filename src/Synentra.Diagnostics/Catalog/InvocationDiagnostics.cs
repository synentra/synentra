namespace Synentra.Diagnostics.Catalog;

public static class InvocationDiagnostics
{
    public static readonly Diagnostic InvalidAuthenticatedAgentId = Define(1, "Authenticated agent id is required.", DiagnosticCategory.Validation);
    public static readonly Diagnostic InvalidOperationId = Define(2, "OperationId cannot be empty.", DiagnosticCategory.Validation);
    public static readonly Diagnostic InvalidBindingDigest = Define(3, "Binding digest must be a 64-character hexadecimal string.", DiagnosticCategory.Validation);
    public static Diagnostic InvocationNotFound(string invocationId) => Define(4, $"Invocation {invocationId} not found.", DiagnosticCategory.NotFound);
    public static readonly Diagnostic CrossAgentResumeDenied = Define(5, "Cross-agent resume denied.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic NoApprovalBoundToInvocation = Define(6, "No approval bound to invocation.", DiagnosticCategory.Conflict);
    public static Diagnostic ApprovalNotInApprovedState(string status) => Define(7, $"Approval not in approved state: {status}.", DiagnosticCategory.Conflict);
    public static Diagnostic AuthorityExpired(string message) => Define(8, message, DiagnosticCategory.Forbidden);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Invocations, sequence, message, category);
}