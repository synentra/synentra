namespace Synentra.Diagnostics.Catalog;

public static class TaskGrantDiagnostics
{
    public static Diagnostic CannotRevoke(string state) => Define(1, $"Cannot revoke in state {state}.", DiagnosticCategory.Conflict);
    public static readonly Diagnostic IssuerCannotGrantZeroOrNegativeAuthority = Define(2, "Issuer cannot grant zero or negative authority.", DiagnosticCategory.Validation);
    public static Diagnostic GrantNotFound(string grantId) => Define(3, $"Grant {grantId} not found.", DiagnosticCategory.NotFound);
    public static readonly Diagnostic OnlyIssuerOrAdminMayRevoke = Define(4, "Only issuer or admin may revoke grant.", DiagnosticCategory.Forbidden);
    public static readonly Diagnostic GrantExpiryMustBeFuture = Define(5, "Grant expiry must be in the future.", DiagnosticCategory.Validation);
    public static readonly Diagnostic GrantOperationScopeRequired = Define(6, "At least one operation scope is required.", DiagnosticCategory.Validation);
    public static readonly Diagnostic TaskGrantNotFound = Define(7, "The requested grant was not found.", DiagnosticCategory.NotFound);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.TaskGrants, sequence, message, category);
}