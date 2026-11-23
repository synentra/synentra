namespace Synentra.Diagnostics.Exceptions;

/// <summary>
/// Thrown when a domain invariant or state-machine rule is violated.
/// Owns the 0001–0999 sequence range of each module in the catalog.
/// </summary>
public sealed class SynentraDomainException : SynentraException
{
    public SynentraDomainException(Diagnostic diagnostic, Exception? inner = null)
        : base(diagnostic, inner) { }
}