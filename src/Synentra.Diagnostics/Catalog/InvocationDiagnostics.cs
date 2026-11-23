namespace Synentra.Diagnostics.Catalog;

public static class InvocationDiagnostics
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

    /// <summary>SYNENTRA-INV-0004</summary>
    public static Diagnostic InvocationNotFound(string invocationId) => Diagnostic.Define(
        module: DiagnosticModules.Invocations,
        category: DiagnosticCategory.NotFound,
        sequence: 4,
        message: $"Invocation {invocationId} not found.");

    /// <summary>SYNENTRA-INV-0005</summary>
    public static readonly Diagnostic CrossAgentResumeDenied = Diagnostic.Define(
        module: DiagnosticModules.Invocations,
        category: DiagnosticCategory.Forbidden,
        sequence: 5,
        message: "Cross-agent resume denied.");

    /// <summary>SYNENTRA-INV-0006</summary>
    public static readonly Diagnostic NoApprovalBoundToInvocation = Diagnostic.Define(
        module: DiagnosticModules.Invocations,
        category: DiagnosticCategory.Conflict,
        sequence: 6,
        message: "No approval bound to invocation.");

    /// <summary>SYNENTRA-INV-0007</summary>
    public static Diagnostic ApprovalNotInApprovedState(string status) => Diagnostic.Define(
        module: DiagnosticModules.Invocations,
        category: DiagnosticCategory.Conflict,
        sequence: 7,
        message: $"Approval not in approved state: {status}.");

    /// <summary>SYNENTRA-INV-0008</summary>
    public static Diagnostic AuthorityExpired(string message) => Diagnostic.Define(
        module: DiagnosticModules.Invocations,
        category: DiagnosticCategory.Forbidden,
        sequence: 8,
        message: message);
}