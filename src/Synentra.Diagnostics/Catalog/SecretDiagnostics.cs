namespace Synentra.Diagnostics.Catalog;

public static class SecretDiagnostics
{
    public static readonly Diagnostic SecretRefEmpty = Define(1, "Secret reference is empty.", DiagnosticCategory.Validation);
    public static readonly Diagnostic SecretRefAmbiguous = Define(2, "Secret reference is ambiguous (looks like a Windows path).", DiagnosticCategory.Validation);
    public static readonly Diagnostic BindingIdMismatch = Define(3, "The provided binding ID does not match the requested ID.", DiagnosticCategory.Conflict);
    public static readonly Diagnostic BindingNotFound = Define(4, "The requested binding was not found.", DiagnosticCategory.NotFound);
    public static readonly Diagnostic InvalidRequest = Define(5, "The provided request is invalid.", DiagnosticCategory.Validation);
    public static readonly Diagnostic InvalidCredentialProvider = Define(6, "The specified credential provider is not valid.", DiagnosticCategory.Validation);
    public static readonly Diagnostic InvalidSecretReference = Define(7, "The specified secret reference is not valid.", DiagnosticCategory.Validation);
    public static readonly Diagnostic CredentialUnavailable = Define(8, "A required credential is unavailable.", DiagnosticCategory.NotFound);

    private static Diagnostic Define(ushort sequence, string message, DiagnosticCategory category = DiagnosticCategory.Validation) =>
        Diagnostic.Define(DiagnosticModules.Secrets, sequence, message, category);
}