namespace Synentra.Diagnostics.Catalog;

public static class ManifestDiagnostics
{
    /// <summary>SYNENTRA-MFT-0001</summary>
    public static readonly Diagnostic ManifestIdMismatch = Diagnostic.Define(
        module: DiagnosticModules.Manifest,
        category: DiagnosticCategory.Conflict,
        sequence: 1,
        message: "The manifest identifier does not match the request.");

    /// <summary>SYNENTRA-MFT-0002</summary>
    public static Diagnostic ActivationConflict(string message) => Diagnostic.Define(
        module: DiagnosticModules.Manifest,
        category: DiagnosticCategory.Validation,
        sequence: 2,
        message: message);

    /// <summary>SYNENTRA-MFT-0003</summary>
    public static readonly Diagnostic PayloadTooLarge = Diagnostic.Define(
        module: DiagnosticModules.Manifest,
        category: DiagnosticCategory.Validation,
        sequence: 3,
        message: "Manifest exceeds the configured size limit.");

    /// <summary>SYNENTRA-MFT-0004</summary>
    public static Diagnostic InvalidManifest(string message) => Diagnostic.Define(
        module: DiagnosticModules.Manifest,
        category: DiagnosticCategory.Validation,
        sequence: 4,
        message: message);

    /// <summary>SYNENTRA-MFT-0005</summary>
    public static readonly Diagnostic NoActiveBundle = Diagnostic.Define(
        module: DiagnosticModules.Manifest,
        category: DiagnosticCategory.NotFound,
        sequence: 5,
        message: "No active governance bundle is available.");
}