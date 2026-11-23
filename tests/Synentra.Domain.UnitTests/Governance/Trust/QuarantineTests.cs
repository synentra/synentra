using FluentAssertions;
using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Governance.Trust;

namespace Synentra.Domain.Tests;

public sealed class QuarantineTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveQuarantine()
    {
        var before = DateTimeOffset.UtcNow;
        var quarantine = new Quarantine("q-1", "agent-1", "tenant-a", "suspicious activity");
        var after = DateTimeOffset.UtcNow;

        quarantine.Id.Should().Be("q-1");
        quarantine.AgentId.Should().Be("agent-1");
        quarantine.SecurityScope.Should().Be("tenant-a");
        quarantine.Reason.Should().Be("suspicious activity");
        quarantine.StartedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        quarantine.ReleasedAt.Should().BeNull();
        quarantine.Active.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "agent", "scope", "reason")]
    [InlineData(" ", "agent", "scope", "reason")]
    [InlineData("id", null, "scope", "reason")]
    [InlineData("id", " ", "scope", "reason")]
    [InlineData("id", "agent", null, "reason")]
    [InlineData("id", "agent", " ", "reason")]
    [InlineData("id", "agent", "scope", null)]
    [InlineData("id", "agent", "scope", " ")]
    public void Constructor_ShouldRejectMissingRequiredValues(string? id, string? agent, string? scope, string? reason)
        => FluentActions.Invoking(() => new Quarantine(id!, agent!, scope!, reason!))
            .Should().Throw<ArgumentException>();

    [Fact]
    public void Release_ShouldMarkQuarantineInactive()
    {
        var quarantine = new Quarantine("q", "agent", "scope", "reason");

        quarantine.Release("reviewer");

        quarantine.Active.Should().BeFalse();
        quarantine.ReleasedAt.Should().NotBeNull();
        quarantine.ReleasedAt!.Value.Should().BeOnOrAfter(quarantine.StartedAt);
    }

    [Fact]
    public void Release_ShouldRejectAlreadyReleasedQuarantine()
    {
        var quarantine = new Quarantine("q", "agent", "scope", "reason");
        quarantine.Release("reviewer");

        FluentActions.Invoking(() => quarantine.Release("another-reviewer"))
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(QuarantineDiagnostics.AlreadyReleased);
    }
}
