namespace Synentra.Diagnostics.Catalog;

public static class BindingDigestDiagnostics
{
    public static readonly Diagnostic InvalidBindingDigest = Define(1, "Binding digest must be a 64-character hexadecimal string.", DiagnosticCategory.Validation);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.BindingDigest, sequence, message, category);
}