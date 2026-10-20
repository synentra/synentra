using Synentra.Domain.Governance.Policies;

namespace Synentra.Application.Features.Policies.Contracts;

public sealed record PolicyResult(
    PolicyOutcome Outcome, 
    IReadOnlyList<string> ReasonCodes, 
    string PolicyRevision, 
    IReadOnlyList<string>? MatchedRuleIds = null)
{
    public static PolicyResult Allow(
        IReadOnlyList<string> reasons, 
        string rev = "unknown", 
        IReadOnlyList<string>? rules = null) => new(PolicyOutcome.Allow, reasons, rev, rules);

    public static PolicyResult Review(
        IReadOnlyList<string> reasons, 
        string rev = "unknown", 
        IReadOnlyList<string>? rules = null) => new(PolicyOutcome.Review, reasons, rev, rules);

    public static PolicyResult Deny(
        IReadOnlyList<string> reasons, 
        string rev = "unknown", 
        IReadOnlyList<string>? rules = null) => new(PolicyOutcome.Deny, reasons, rev, rules);
}
