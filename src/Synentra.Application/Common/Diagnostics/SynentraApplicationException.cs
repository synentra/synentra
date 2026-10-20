using Synentra.Domain.Common.Diagnostics;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Application.Common.Diagnostics;

public sealed class SynentraApplicationException : SynentraException
{
    public SynentraApplicationException(Diagnostic diagnostic, Exception? inner = null)
        : base(diagnostic, inner)
    {
    }
}