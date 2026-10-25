using Synentra.Domain.Common.Diagnostics;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Application.Common.Security;

public sealed class CredentialNotFoundException : SynentraException
{
    public CredentialNotFoundException(Diagnostic diagnostic,
        Exception? inner = null)
        : base(diagnostic, inner)
    {
    }
}
