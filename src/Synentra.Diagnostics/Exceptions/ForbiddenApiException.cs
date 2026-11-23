namespace Synentra.Diagnostics.Exceptions;

public sealed class ForbiddenApiException : SynentraException
{
    public ForbiddenApiException(Diagnostic diagnostic, Exception? inner = null)
        : base(diagnostic, inner)
    {
    }
}