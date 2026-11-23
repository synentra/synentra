namespace Synentra.Diagnostics.Catalog;

public static class ResourceDiagnostics
{
    /// <summary>SYNENTRA-RSD-0001</summary>
    public static readonly Diagnostic MissingResourceType = Diagnostic.Define(
        module: DiagnosticModules.ResourceDescriptor,
        category: DiagnosticCategory.Validation,
        sequence: 1,
        message: "Resource type required.");

    /// <summary>SYNENTRA-RSD-0002</summary>
    public static readonly Diagnostic InvalidResourceType = Diagnostic.Define(
        module: DiagnosticModules.ResourceDescriptor,
        category: DiagnosticCategory.Validation,
        sequence: 2,
        message: "Resource type is invalid.");

    /// <summary>SYNENTRA-RSD-0003</summary>
    public static readonly Diagnostic InvalidResourceId = Diagnostic.Define(
        module: DiagnosticModules.ResourceDescriptor,
        category: DiagnosticCategory.Validation,
        sequence: 3,
        message: "Resource id is invalid.");

    /// <summary>SYNENTRA-RSD-0004</summary>
    public static readonly Diagnostic InvalidSecurityScope = Diagnostic.Define(
        module: DiagnosticModules.ResourceDescriptor,
        category: DiagnosticCategory.Validation,
        sequence: 4,
        message: "Security scope is invalid.");

    /// <summary>SYNENTRA-RSD-0005</summary>
    public static readonly Diagnostic InvalidAuthorityDomain = Diagnostic.Define(
        module: DiagnosticModules.ResourceDescriptor,
        category: DiagnosticCategory.Validation,
        sequence: 5,
        message: "Authority domain is invalid.");
}