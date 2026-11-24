namespace Synentra.Diagnostics.Catalog;

public static class ManifestDiagnostics
{
    public static readonly Diagnostic ManifestIdMismatch = Define(1, "The manifest identifier does not match the request.", DiagnosticCategory.Conflict);
    public static Diagnostic ActivationConflict(string message) => Define(2, message, DiagnosticCategory.Validation);
    public static readonly Diagnostic PayloadTooLarge = Define(3, "Manifest exceeds the configured size limit.", DiagnosticCategory.Validation);
    public static Diagnostic InvalidManifest(string message) => Define(4, message, DiagnosticCategory.Validation);
    public static readonly Diagnostic NoActiveBundle = Define(5, "No active governance bundle is available.", DiagnosticCategory.NotFound);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Manifest, sequence, message, category);
}