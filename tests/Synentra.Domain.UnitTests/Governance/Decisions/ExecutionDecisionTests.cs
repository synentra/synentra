using FluentAssertions;
using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Governance.Evidence;

namespace Synentra.Domain.Tests;

public sealed class ExecutionDecisionTests
{
    [Fact]
    public void Allow_ShouldUseProvidedEvidenceRevisionAndTtl()
    {
        var evidence = new GovernanceEvidence { ManifestRevision = "manifest-1" };
        var before = DateTimeOffset.UtcNow;
        var decision = ExecutionDecision.Allow(["SAFE"], evidence, "policy-1", TimeSpan.FromMinutes(3));
        var after = DateTimeOffset.UtcNow;

        decision.Outcome.Should().Be(DecisionOutcome.Allow);
        decision.ReasonCodes.Should().Equal("SAFE");
        decision.Evidence.Should().BeSameAs(evidence);
        decision.PolicyRevision.Should().Be("policy-1");
        decision.ExpiresAt.Should().BeOnOrAfter(before.AddMinutes(3)).And.BeOnOrBefore(after.AddMinutes(3));
        decision.ObligationId.Should().BeNull();
    }

    [Fact]
    public void Allow_ShouldUseDefaultEvidenceRevisionAndTtl()
    {
        var before = DateTimeOffset.UtcNow;
        var decision = ExecutionDecision.Allow([]);
        var after = DateTimeOffset.UtcNow;
        decision.Outcome.Should().Be(DecisionOutcome.Allow);
        decision.Evidence.Should().BeOfType<GovernanceEvidence>();
        decision.PolicyRevision.Should().Be("unknown");
        decision.ExpiresAt.Should().BeOnOrAfter(before.AddMinutes(15)).And.BeOnOrBefore(after.AddMinutes(15));
        decision.ReasonCodes.Should().BeEmpty();
    }

    [Fact]
    public void Review_ShouldUseProvidedEvidenceAndCustomTtl()
    {
        var evidence = new GovernanceEvidence();
        var before = DateTimeOffset.UtcNow;
        var decision = ExecutionDecision.Review(["NEEDS_REVIEW"], evidence, "rev-2", TimeSpan.FromSeconds(30));
        var after = DateTimeOffset.UtcNow;

        decision.Outcome.Should().Be(DecisionOutcome.Review);
        decision.ReasonCodes.Should().Equal("NEEDS_REVIEW");
        decision.Evidence.Should().BeSameAs(evidence);
        decision.PolicyRevision.Should().Be("rev-2");
        decision.ExpiresAt.Should().BeOnOrAfter(before.AddSeconds(30)).And.BeOnOrBefore(after.AddSeconds(30));
        decision.ObligationId.Should().BeNull();
    }

    [Fact]
    public void Review_ShouldUseDefaultsWhenEvidenceAndTtlAreOmitted()
    {
        var before = DateTimeOffset.UtcNow;
        var decision = ExecutionDecision.Review([]);
        var after = DateTimeOffset.UtcNow;

        decision.Outcome.Should().Be(DecisionOutcome.Review);
        decision.Evidence.Should().BeOfType<GovernanceEvidence>();
        decision.PolicyRevision.Should().Be("unknown");
        decision.ExpiresAt.Should().BeOnOrAfter(before.AddMinutes(15)).And.BeOnOrBefore(after.AddMinutes(15));
    }

    [Fact]
    public void Deny_ShouldExpireImmediatelyAndUseDefaultEvidenceWhenOmitted()
    {
        var before = DateTimeOffset.UtcNow;
        var decision = ExecutionDecision.Deny(["BLOCKED"]);
        var after = DateTimeOffset.UtcNow;

        decision.Outcome.Should().Be(DecisionOutcome.Deny);
        decision.ReasonCodes.Should().Equal("BLOCKED");
        decision.Evidence.Should().BeOfType<GovernanceEvidence>();
        decision.PolicyRevision.Should().Be("unknown");
        decision.ExpiresAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        decision.ObligationId.Should().BeNull();
    }

    [Fact]
    public void Deny_ShouldPreserveProvidedEvidenceAndRevision()
    {
        var evidence = new GovernanceEvidence();
        var decision = ExecutionDecision.Deny(["DENIED"], evidence, "rev-3");
        decision.Evidence.Should().BeSameAs(evidence);
        decision.PolicyRevision.Should().Be("rev-3");
        decision.Outcome.Should().Be(DecisionOutcome.Deny);
    }
}
