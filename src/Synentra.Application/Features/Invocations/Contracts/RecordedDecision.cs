namespace Synentra.Application.Features.Invocations.Contracts;

public sealed record RecordedDecision(
    string Outcome, 
    IReadOnlyList<string> ReasonCodes,
    string PolicyRevision,
    DateTimeOffset ExpiresAt);
