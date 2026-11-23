namespace Synentra.Diagnostics.Catalog;

public static class TaskGrantDiagnostics
{
    /// <summary>SYNENTRA-TGR-0001</summary>
    public static Diagnostic CannotRevoke(string state) => Diagnostic.Define(
        module: DiagnosticModules.TaskGrants,
        category: DiagnosticCategory.Conflict,
        sequence: 1,
        message: $"Cannot revoke in state {state}.");

    /// <summary>SYNENTRA-TGR-0002</summary>
    public static readonly Diagnostic IssuerCannotGrantZeroOrNegativeAuthority = Diagnostic.Define(
        module: DiagnosticModules.TaskGrants,
        category: DiagnosticCategory.Validation,
        sequence: 2,
        message: "Issuer cannot grant zero or negative authority.");

    /// <summary>SYNENTRA-TGR-0003</summary>
    public static Diagnostic GrantNotFound(string grantId) => Diagnostic.Define(
        module: DiagnosticModules.TaskGrants,
        category: DiagnosticCategory.NotFound,
        sequence: 3,
        message: $"Grant {grantId} not found.");

    /// <summary>SYNENTRA-TGR-0004</summary>
    public static readonly Diagnostic OnlyIssuerOrAdminMayRevoke = Diagnostic.Define(
        module: DiagnosticModules.TaskGrants,
        category: DiagnosticCategory.Forbidden,
        sequence: 4,
        message: "Only issuer or admin may revoke grant.");

    /// <summary>SYNENTRA-TGR-0005</summary>
    public static readonly Diagnostic GrantExpiryMustBeFuture = Diagnostic.Define(
        module: DiagnosticModules.TaskGrants,
        category: DiagnosticCategory.Validation,
        sequence: 5,
        message: "Grant expiry must be in the future.");

    /// <summary>SYNENTRA-TGR-0006</summary>
    public static readonly Diagnostic GrantOperationScopeRequired = Diagnostic.Define(
        module: DiagnosticModules.TaskGrants,
        category: DiagnosticCategory.Validation,
        sequence: 6,
        message: "At least one operation scope is required.");

    /// <summary>SYNENTRA-TGR-0007</summary>
    public static readonly Diagnostic TaskGrantNotFound = Diagnostic.Define(
        module: DiagnosticModules.TaskGrants,
        category: DiagnosticCategory.NotFound,
        sequence: 7,
        message: "The requested grant was not found.");
}