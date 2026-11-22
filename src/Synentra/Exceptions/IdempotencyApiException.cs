using Synentra.Domain.Common.Diagnostics;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Exceptions;

public sealed class IdempotencyApiException : SynentraException
{
    public IdempotencyApiException(Diagnostic diagnostic, Exception? inner = null)
        : base(diagnostic, inner)
    {
    }
}