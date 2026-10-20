using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Invocations.ValueObjects;

namespace Synentra.Domain.Invocations.Events;

public sealed record InvocationDecisionMade(
    InvocationId InvocationId, 
    DecisionOutcome Outcome, 
    IReadOnlyList<string> ReasonCodes, 
    string PolicyRevision, 
    DateTimeOffset OccurredAt);
