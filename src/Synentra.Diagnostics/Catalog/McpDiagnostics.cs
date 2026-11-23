namespace Synentra.Diagnostics.Catalog;

public static class McpDiagnostics
{
    public static readonly Diagnostic ParseError = Define(1, "Parse error.");
    public static readonly Diagnostic InvalidRequest = Define(2, "Invalid Request");
    public static Diagnostic UnsupportedProtocolVersion(string protocolVersion) => Define(3, $"Unsupported protocol version. Synentra requires {protocolVersion}.");
    public static readonly Diagnostic UnknownServerRoute = Define(4, "Unknown MCP server route.");
    public static readonly Diagnostic InvalidSession = Define(5, "Missing or invalid MCP session.");
    public static readonly Diagnostic NoActiveBundle = Define(6, "No active governance bundle.");
    public static readonly Diagnostic ToolNameRequired = Define(7, "Tool name is required.");
    public static readonly Diagnostic UnknownTool = Define(8, "Unknown tool.");
    public static readonly Diagnostic AmbiguousTool = Define(9, "Ambiguous tool binding.");
    public static readonly Diagnostic ApprovalUnavailable = Define(10, "Approval is not available for this caller.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic ApprovalExpired = Define(11, "Approval has expired.", DiagnosticCategory.Conflict);
    public static readonly Diagnostic ApprovalDifferentOperation = Define(12, "Approval is bound to a different operation.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic ApprovedInvocationMissing = Define(13, "Approved invocation no longer exists.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic ReviewedArgumentsMismatch = Define(14, "Reviewed arguments do not match the approved invocation.", DiagnosticCategory.Forbidden);
    public static Diagnostic ApprovalStatus(string status) => Define(15, $"Approval is {status}.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic InvocationNotAdmissible = Define(16, "Invocation is no longer admissible.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic ApprovalAuthorityExpired = Define(17, "Approval authority expired.", DiagnosticCategory.Conflict);
    public static readonly Diagnostic DispatchPending = Define(18, "Invocation admitted; dispatch is pending.", DiagnosticCategory.Infrastructure);
    public static readonly Diagnostic IdempotencyKeyRequired = Define(19, "Mutation tools require io.synentra/idempotencyKey metadata.");
    public static readonly Diagnostic InvocationDenied = Define(20, "Synentra denied the tool invocation.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic HumanReviewRequired = Define(21, "Human review is required.", DiagnosticCategory.Conflict);
    public static readonly Diagnostic MethodNotFound = Define(22, "Method not found", DiagnosticCategory.NotFound);
    public static readonly Diagnostic InvocationCancelled = Define(23, "Invocation was cancelled before dispatch.", DiagnosticCategory.Conflict);
    public static readonly Diagnostic InvocationFailed = Define(24, "Invocation failed before dispatch.", DiagnosticCategory.Infrastructure);
    public static readonly Diagnostic DispatchUncertain = Define(25, "Dispatch outcome is uncertain; query the invocation before retrying.", DiagnosticCategory.Infrastructure);
    public static Diagnostic DispatchStillPending(string status) => Define(26, $"Dispatch is still pending.", DiagnosticCategory.Infrastructure);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Mcp, sequence, message, category);
}