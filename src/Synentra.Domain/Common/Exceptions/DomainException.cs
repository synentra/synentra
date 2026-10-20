using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Domain.Common.Exceptions;

public sealed class DomainException : SynentraException
{
    public DomainException(Diagnostic diagnostic,
        Exception? inner = null)
        : base(diagnostic, inner)
    {
    }
}