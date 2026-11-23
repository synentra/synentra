using FluentAssertions;
using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Approvals;
using Synentra.Domain.Approvals.Events;
using Synentra.Domain.Invocations.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class ApprovalTests
{
    private static readonly InvocationId Invocation = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly BindingDigest Digest = new(new string('A', 64));

    private static Approval CreateApproval(DateTimeOffset? expiresAt = null)
        => new("approval-1", Invocation, Digest, "agent-1", "inventory.read", "binding-1",
            "grant-1", "policy-v1", "review-v1", "tenant-a", expiresAt ?? DateTimeOffset.UtcNow.AddHours(1));

    [Fact]
    public void Constructor_ShouldInitializePendingApproval()
    {
        var before = DateTimeOffset.UtcNow;
        var approval = CreateApproval();
        var after = DateTimeOffset.UtcNow;

        approval.Id.Should().Be("approval-1");
        approval.InvocationId.Should().Be(Invocation);
        approval.BindingDigest.Should().Be(Digest);
        approval.RequestingAgentId.Should().Be("agent-1");
        approval.OperationId.Should().Be("inventory.read");
        approval.BindingId.Should().Be("binding-1");
        approval.TaskGrantId.Should().Be("grant-1");
        approval.PolicyRevision.Should().Be("policy-v1");
        approval.ReviewObligationRevision.Should().Be("review-v1");
        approval.SecurityScope.Should().Be("tenant-a");
        approval.Status.Should().Be(ApprovalStatus.Pending);
        approval.ReviewerId.Should().BeNull();
        approval.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        approval.ExpiresAt.Should().BeAfter(approval.CreatedAt);
        approval.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public void Approve_ShouldResolvePendingNonExpiredApproval()
    {
        var approval = CreateApproval();

        approval.Approve("reviewer-1");

        approval.Status.Should().Be(ApprovalStatus.Approved);
        approval.ReviewerId.Should().Be("reviewer-1");
        approval.ResolvedAt.Should().NotBeNull();
        approval.ResolvedAt!.Value.Should().BeOnOrAfter(approval.CreatedAt);
    }

    [Fact]
    public void Approve_ShouldRejectExpiredApproval()
    {
        var approval = CreateApproval(DateTimeOffset.UtcNow.AddSeconds(-1));

        FluentActions.Invoking(() => approval.Approve("reviewer-1"))
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(ApprovalDiagnostics.ApprovalHasExpired);
        approval.Status.Should().Be(ApprovalStatus.Pending);
        approval.ReviewerId.Should().BeNull();
    }

    [Fact]
    public void Approve_ShouldRejectNonPendingApproval()
    {
        var approval = Restore(ApprovalStatus.Rejected);

        FluentActions.Invoking(() => approval.Approve("reviewer-1"))
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(ApprovalDiagnostics.CannotApprove(ApprovalStatus.Rejected.ToString()));
    }

    [Fact]
    public void Reject_ShouldResolvePendingApproval()
    {
        var approval = CreateApproval();

        approval.Reject("reviewer-2");

        approval.Status.Should().Be(ApprovalStatus.Rejected);
        approval.ReviewerId.Should().Be("reviewer-2");
        approval.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_ShouldRejectNonPendingApproval()
    {
        var approval = Restore(ApprovalStatus.Approved);

        FluentActions.Invoking(() => approval.Reject("reviewer-2"))
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(ApprovalDiagnostics.CannotReject(ApprovalStatus.Approved.ToString()));
    }

    [Theory]
    [InlineData(ApprovalStatus.Rejected)]
    [InlineData(ApprovalStatus.Expired)]
    public void Revoke_ShouldRejectRejectedOrExpiredApproval(ApprovalStatus status)
    {
        var approval = Restore(status);

        FluentActions.Invoking(approval.Revoke)
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(ApprovalDiagnostics.CannotRevoke(status.ToString()));
    }

    [Theory]
    [InlineData(ApprovalStatus.Pending)]
    [InlineData(ApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.Consumed)]
    public void Revoke_ShouldRevokeAnyStateOtherThanRejectedOrExpired(ApprovalStatus initialStatus)
    {
        var approval = Restore(initialStatus);

        approval.Revoke();

        approval.Status.Should().Be(ApprovalStatus.Revoked);
        approval.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Consume_ShouldTransitionApprovedApproval()
    {
        var approval = Restore(ApprovalStatus.Approved);

        approval.Consume();

        approval.Status.Should().Be(ApprovalStatus.Consumed);
    }

    [Theory]
    [InlineData(ApprovalStatus.Pending)]
    [InlineData(ApprovalStatus.Rejected)]
    [InlineData(ApprovalStatus.Revoked)]
    [InlineData(ApprovalStatus.Expired)]
    [InlineData(ApprovalStatus.Consumed)]
    public void Consume_ShouldRejectNonApprovedApproval(ApprovalStatus status)
    {
        var approval = Restore(status);

        FluentActions.Invoking(approval.Consume)
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(ApprovalDiagnostics.CannotConsume(status.ToString()));
    }

    [Fact]
    public void Restore_ShouldPreservePersistedState()
    {
        var created = DateTimeOffset.Parse("2026-01-01T10:00:00Z");
        var expires = created.AddHours(2);
        var resolved = created.AddMinutes(10);

        var approval = Approval.Restore("restored", Invocation, Digest, "agent", "op", "binding",
            null, "policy", "review", "scope", expires, created, ApprovalStatus.Approved, "reviewer", resolved);

        approval.Id.Should().Be("restored");
        approval.TaskGrantId.Should().BeNull();
        approval.CreatedAt.Should().Be(created);
        approval.ExpiresAt.Should().Be(expires);
        approval.Status.Should().Be(ApprovalStatus.Approved);
        approval.ReviewerId.Should().Be("reviewer");
        approval.ResolvedAt.Should().Be(resolved);
    }

    [Fact]
    public void ApprovalEvents_ShouldExposeConstructorData()
    {
        var now = DateTimeOffset.UtcNow;
        var requested = new ApprovalRequested(Invocation, "approval", Digest, now.AddMinutes(5));
        var approved = new ApprovalApproved("approval", "reviewer", now);
        var rejected = new ApprovalRejected("approval", "reviewer", "not permitted", now);
        var rejectedWithoutReason = new ApprovalRejected("approval", "reviewer", null, now);

        requested.InvocationId.Should().Be(Invocation);
        requested.ApprovalId.Should().Be("approval");
        requested.BindingDigest.Should().Be(Digest);
        requested.ExpiresAt.Should().Be(now.AddMinutes(5));
        approved.ApprovalId.Should().Be("approval");
        approved.ReviewerId.Should().Be("reviewer");
        approved.OccurredAt.Should().Be(now);
        rejected.ApprovalId.Should().Be("approval");
        rejected.ReviewerId.Should().Be("reviewer");
        rejected.Reason.Should().Be("not permitted");
        rejected.OccurredAt.Should().Be(now);
        rejectedWithoutReason.Reason.Should().BeNull();
    }

    private static Approval Restore(ApprovalStatus status)
        => Approval.Restore("approval", Invocation, Digest, "agent", "op", "binding", null,
            "policy", "review", "scope", DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddMinutes(-1),
            status, null, null);
}
