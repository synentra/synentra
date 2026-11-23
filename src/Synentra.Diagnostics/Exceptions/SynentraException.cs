namespace Synentra.Diagnostics.Exceptions;

/// <summary>
/// Base type for every exception raised by Synentra.
/// Every instance carries a <see cref="Diagnostic"/> that identifies
/// the failure by a stable, published code.
/// </summary>
/// <remarks>
/// This type is abstract; the four sealed subclasses correspond to the
/// four architectural layers. Do <b>not</b> derive from the sealed types —
/// derive from this base only if you are adding a new layer, and update
/// <see cref="SynentraLayer"/> and the exception handler in lockstep.
/// </remarks>
public abstract class SynentraException : Exception
{
    public Diagnostic Diagnostic { get; }
    public string Code => Diagnostic.Code;
    public DiagnosticCategory Category => Diagnostic.Category;

    protected SynentraException(Diagnostic diagnostic, Exception? inner = null)
    : base(FormatMessage(diagnostic), inner)
    {
        Diagnostic = diagnostic;
    }

    // Prepends the code so logs read like:
    //   [SYNENTRA-APR-1001] Approval 42 not found.
    // The Problem Details response uses Diagnostic.Message directly,
    // so clients still see the clean message without the prefix.
    private static string FormatMessage(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return $"[{diagnostic.Code}] {diagnostic.Message}";
    }

    public override string ToString() => $"{Diagnostic}{Environment.NewLine}{base.ToString()}";
}