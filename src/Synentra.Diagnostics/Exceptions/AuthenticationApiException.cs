namespace Synentra.Diagnostics.Exceptions;

public sealed class AuthenticationApiException : SynentraException
{
    public AuthenticationApiException(Diagnostic diagnostic, Exception? inner = null)
        : base(diagnostic, inner)
    {
    }
}