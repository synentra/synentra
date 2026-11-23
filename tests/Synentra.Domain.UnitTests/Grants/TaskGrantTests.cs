using FluentAssertions;
using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Grants;
using Synentra.Domain.Grants.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class TaskGrantTests
{
    private static TaskGrant Create(DateTimeOffset? expiresAt = null)
        => new("grant-1", "issuer-1", "agent-1", "tenant-a", ["inventory.read", "inventory.update"],
            [new ResourceScope("target", "product", ["p-1", "p-2"])],
            expiresAt ?? DateTimeOffset.UtcNow.AddHours(1), "external-1");

    [Fact]
    public void Constructor_ShouldInitializeActiveGrant()
    {
        var before = DateTimeOffset.UtcNow;
        var grant = Create();
        var after = DateTimeOffset.UtcNow;

        grant.Id.Should().Be("grant-1");
        grant.IssuerId.Should().Be("issuer-1");
        grant.AgentId.Should().Be("agent-1");
        grant.SecurityScope.Should().Be("tenant-a");
        grant.OperationIds.Should().Equal("inventory.read", "inventory.update");
        grant.ResourceScopes.Should().ContainSingle();
        grant.ExpiresAt.Should().BeAfter(after);
        grant.ExternalReference.Should().Be("external-1");
        grant.State.Should().Be(GrantState.Active);
        grant.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);

        var resourceScope = grant.ResourceScopes.Single();
        resourceScope.Role.Should().Be("target");
        resourceScope.Type.Should().Be("product");
        resourceScope.Ids.Should().Equal("p-1", "p-2");
    }

    [Fact]
    public void IsValidAt_ShouldBeTrueOnlyWhileActiveAndBeforeExpiry()
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(10);
        var grant = Create(expires);

        grant.IsValidAt(expires.AddTicks(-1)).Should().BeTrue();
        grant.IsValidAt(expires).Should().BeFalse();
        grant.IsValidAt(expires.AddTicks(1)).Should().BeFalse();
        grant.IsValidAt(DateTimeOffset.UtcNow.AddMinutes(-1)).Should().BeTrue();

        grant.Revoke();
        grant.IsValidAt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void Revoke_ShouldTransitionActiveGrant()
    {
        var grant = Create();
        grant.Revoke();
        grant.State.Should().Be(GrantState.Revoked);

        FluentActions.Invoking(grant.Revoke)
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(TaskGrantDiagnostics.CannotRevoke(GrantState.Revoked.ToString()));
    }

    [Fact]
    public void Revoke_ShouldRejectExpiredState()
    {
        var grant = Restore(GrantState.Expired);
        FluentActions.Invoking(grant.Revoke)
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(TaskGrantDiagnostics.CannotRevoke(GrantState.Expired.ToString()));
    }

    [Fact]
    public void Restore_ShouldPreservePersistedStateAndOptionalReference()
    {
        var created = DateTimeOffset.Parse("2026-06-01T12:00:00Z");
        var expires = created.AddDays(1);
        var resources = new[] { new ResourceScope("subject", "user", ["user-1"]) };
        var grant = TaskGrant.Restore("restored", "issuer", "agent", "scope", ["op"], resources,
            expires, null, GrantState.Expired, created);

        grant.Id.Should().Be("restored");
        grant.IssuerId.Should().Be("issuer");
        grant.AgentId.Should().Be("agent");
        grant.SecurityScope.Should().Be("scope");
        grant.OperationIds.Should().Equal("op");
        grant.ResourceScopes.Should().BeSameAs(resources);
        grant.ExpiresAt.Should().Be(expires);
        grant.ExternalReference.Should().BeNull();
        grant.State.Should().Be(GrantState.Expired);
        grant.CreatedAt.Should().Be(created);
    }

    private static TaskGrant Restore(GrantState state)
        => TaskGrant.Restore("grant", "issuer", "agent", "scope", [], [], DateTimeOffset.UtcNow.AddHours(1),
            null, state, DateTimeOffset.UtcNow.AddMinutes(-1));
}
