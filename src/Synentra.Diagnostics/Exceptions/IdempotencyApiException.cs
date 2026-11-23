namespace Synentra.Diagnostics.Exceptions;

public sealed class IdempotencyApiException : SynentraException
{
    public IdempotencyApiException(Diagnostic diagnostic, Exception? inner = null)
        : base(diagnostic, inner)
    {
    }
}