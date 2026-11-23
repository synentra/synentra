namespace Synentra.Diagnostics.Exceptions;

public sealed class CredentialNotFoundException : SynentraException
{
    public CredentialNotFoundException(Diagnostic diagnostic,
        Exception? inner = null)
        : base(diagnostic, inner)
    {
    }
}
