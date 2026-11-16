using FluentAssertions;
using Synentra.Domain.Governance.Evidence;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Domain.Governance.Risk.ValueObjects;
using Synentra.Domain.Governance.Trust.ValueObjects;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class GovernanceEvidenceTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmptyResourcesAndCollectionTime()
    {
        var before = DateTimeOffset.UtcNow;
        var evidence = new GovernanceEvidence();
        var after = DateTimeOffset.UtcNow;

        evidence.ManifestRevision.Should().BeEmpty();
        evidence.Intent.Should().BeNull();
        evidence.Risk.Should().BeNull();
        evidence.Trust.Should().BeNull();
        evidence.Resources.Should().BeEmpty();
        evidence.CollectedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void InitProperties_ShouldAcceptEvidenceSnapshot()
    {
        var resource = new ResourceDescriptor("product", "p-1", "tenant-a", "inventory");
        var intent = new IntentLabel("safe_read", 0.9, IntentStatus.Available, "v1");
        var risk = new RiskAssessment(new RiskScore(0.1, 1), [], "risk-v1", []);
        var trust = TrustSnapshot.Unknown;
        var collectedAt = DateTimeOffset.Parse("2026-04-01T12:00:00Z");

        var evidence = new GovernanceEvidence
        {
            ManifestRevision = "manifest-v3",
            Intent = intent,
            Risk = risk,
            Trust = trust,
            Resources = [resource],
            CollectedAt = collectedAt
        };

        evidence.ManifestRevision.Should().Be("manifest-v3");
        evidence.Intent.Should().Be(intent);
        evidence.Risk.Should().Be(risk);
        evidence.Trust.Should().Be(trust);
        evidence.Resources.Should().ContainSingle().Which.Should().Be(resource);
        evidence.CollectedAt.Should().Be(collectedAt);
    }
}
