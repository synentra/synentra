namespace Synentra.Application.Features.Invocations.Contracts;

public sealed record DecisionDto(string Outcome, IReadOnlyList<string> ReasonCodes, string PolicyRevision);
