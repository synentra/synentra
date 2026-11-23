namespace Synentra.Diagnostics.Exceptions;

/// <summary>
/// Thrown when an application workflow, policy, or repository outcome
/// rejects the operation. Owns the 1001–1999 sequence range of each module.
/// </summary>
public sealed class SynentraApplicationException : SynentraException
{
    public SynentraApplicationException(Diagnostic diagnostic, Exception? inner = null)
        : base(diagnostic, inner) { }
}