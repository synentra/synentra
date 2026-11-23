namespace Synentra.Diagnostics.Exceptions;

/// <summary>
/// Thrown when an external system (provider, database, message broker)
/// fails in a way the caller cannot fix. Owns the 2001–2999 sequence range
/// of each module. Callers may retry; the handler emits 503.
/// </summary>
public sealed class SynentraInfrastructureException : SynentraException
{
    public SynentraInfrastructureException(Diagnostic diagnostic, Exception? inner = null)
        : base(diagnostic, inner) { }
}