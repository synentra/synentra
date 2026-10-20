namespace Synentra.Application.Features.Manifests.Exceptions;

public sealed class ActivationConflictException : Exception
{
    public string CurrentRevision { get; }
    public string? ExpectedRevision { get; }

    public ActivationConflictException(string current, string? expected) : base($"Activation conflict. Current: '{current}', expected: '{expected}'.")
    {
        CurrentRevision = current;
        ExpectedRevision = expected;
    }
}
