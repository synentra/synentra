using FluentAssertions;
using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;
using Synentra.Domain.Governance.Trust;
using Synentra.Domain.Governance.Trust.Events;
using Synentra.Domain.Governance.Trust.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class TrustValueObjectTests
{
    [Theory]
    [InlineData(0d)]
    [InlineData(50d)]
    [InlineData(100d)]
    public void TrustScore_ShouldAcceptInclusiveRange(double value)
        => new TrustScore(value).Value.Should().Be(value);

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(100.01d)]
    public void TrustScore_ShouldRejectOutOfRangeValues(double value)
        => FluentActions.Invoking(() => new TrustScore(value))
            .Should().Throw<DomainException>()
            .Which.Diagnostic.Should().Be(DomainErrors.Trust.TrustScoreOutOfRange);

    [Fact]
    public void TrustSnapshot_Unknown_ShouldProvideSafeDefaults()
    {
        var before = DateTimeOffset.UtcNow;
        var snapshot = TrustSnapshot.Unknown;
        var after = DateTimeOffset.UtcNow;
        snapshot.Score.Value.Should().Be(0);
        snapshot.Status.Should().Be(TrustStatus.Unknown);
        snapshot.Version.Should().Be("0");
        snapshot.Quarantined.Should().BeFalse();
        snapshot.Factors.Should().BeEmpty();
        snapshot.SnapshotAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void TrustRecords_ShouldExposeTheirValues()
    {
        var now = DateTimeOffset.UtcNow;
        var metadata = new Dictionary<string, string> { ["source"] = "audit" };
        var trustEvidence = new TrustEvidence("agent", "scope", TrustEvidenceType.ManualAdjustment,
            "evidence-1", "reviewed", now, metadata);
        var trustEvidenceWithoutMetadata = new TrustEvidence("agent", "scope", TrustEvidenceType.ReviewerAction,
            "evidence-2", "approved", now);
        var factor = new TrustFactor("reliability", 0.8, "consistent successful calls");
        var snapshot = new TrustSnapshot(new TrustScore(80), TrustStatus.Elevated, "v2", false, [factor], now);
        var changed = new TrustScoreChanged("agent", 50, 80, "positive history", now);

        trustEvidence.AgentId.Should().Be("agent");
        trustEvidence.SecurityScope.Should().Be("scope");
        trustEvidence.Type.Should().Be(TrustEvidenceType.ManualAdjustment);
        trustEvidence.EvidenceId.Should().Be("evidence-1");
        trustEvidence.Description.Should().Be("reviewed");
        trustEvidence.ObservedAt.Should().Be(now);
        trustEvidence.Metadata.Should().BeSameAs(metadata);
        trustEvidenceWithoutMetadata.Metadata.Should().BeNull();
        factor.Name.Should().Be("reliability");
        factor.Value.Should().Be(0.8);
        factor.Explanation.Should().Be("consistent successful calls");
        snapshot.Score.Value.Should().Be(80);
        snapshot.Status.Should().Be(TrustStatus.Elevated);
        snapshot.Version.Should().Be("v2");
        snapshot.Quarantined.Should().BeFalse();
        snapshot.Factors.Should().ContainSingle().Which.Should().Be(factor);
        snapshot.SnapshotAt.Should().Be(now);
        changed.AgentId.Should().Be("agent");
        changed.OldScore.Should().Be(50);
        changed.NewScore.Should().Be(80);
        changed.Reason.Should().Be("positive history");
        changed.OccurredAt.Should().Be(now);
    }
}
