namespace Synentra.Diagnostics.Catalog;

public static class BindingDigestDiagnostics
{
    /// <summary>SYNENTRA-BDG-0001</summary>
    public static readonly Diagnostic InvalidBindingDigest = Diagnostic.Define(
        module: DiagnosticModules.BindingDigest,
        category: DiagnosticCategory.Validation,
        sequence: 1,
        message: "Binding digest must be a 64-character hexadecimal string.");
}