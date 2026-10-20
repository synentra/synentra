namespace Synentra.Application.Features.Manifests.Exceptions;

public sealed class BundleParseException : Exception
{
    public IReadOnlyList<string> Issues { get; }
    public BundleParseException(string message, IReadOnlyList<string> issues) : base(message) => Issues = issues;
}
