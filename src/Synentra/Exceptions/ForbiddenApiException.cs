using Synentra.Domain.Common.Diagnostics;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Exceptions;

public sealed class ForbiddenApiException : SynentraException
{
    public ForbiddenApiException(Diagnostic diagnostic, Exception? inner = null)
        : base(diagnostic, inner)
    {
    }
}