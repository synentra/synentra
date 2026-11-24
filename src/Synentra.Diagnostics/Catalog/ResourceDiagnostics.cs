namespace Synentra.Diagnostics.Catalog;

public static class ResourceDiagnostics
{
    public static readonly Diagnostic MissingResourceType = Define(1, "Resource type required.", DiagnosticCategory.Validation);
    public static readonly Diagnostic InvalidResourceType = Define(2, "Resource type is invalid.", DiagnosticCategory.Validation);
    public static readonly Diagnostic InvalidResourceId = Define(3, "Resource id is invalid.", DiagnosticCategory.Validation);
    public static readonly Diagnostic InvalidSecurityScope = Define(4, "Security scope is invalid.", DiagnosticCategory.Validation);
    public static readonly Diagnostic InvalidAuthorityDomain = Define(5, "Authority domain is invalid.", DiagnosticCategory.Validation);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.ResourceDescriptor, sequence, message, category);
}