using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Domain.Common.Exceptions;

public abstract class SynentraException : Exception
{
    public Diagnostic Diagnostic { get; }

    protected SynentraException(
        Diagnostic diagnostic,
        Exception? inner = null)
        : base((diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))).Message, inner)
    {
        Diagnostic = diagnostic;
    }

    public override string ToString() => $"{Diagnostic}{Environment.NewLine}{base.ToString()}";
}