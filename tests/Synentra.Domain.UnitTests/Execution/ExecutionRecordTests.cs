using FluentAssertions;
using Synentra.Domain.Execution;
using Synentra.Domain.Execution.Events;
using Synentra.Domain.Execution.Evidence;
using Synentra.Domain.Invocations.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class ExecutionRecordTests
{
    private static readonly InvocationId Invocation = InvocationId.New();

    private static ExecutionRecord Create() => new("execution-1", Invocation);

    [Fact]
    public void Constructor_ShouldStartNotStarted()
    {
        var record = Create();
        record.Id.Should().Be("execution-1");
        record.InvocationId.Should().Be(Invocation);
        record.Status.Should().Be(ExecutionStatus.NotStarted);
        record.UpstreamReference.Should().BeNull();
        record.DispatchedAt.Should().BeNull();
        record.RespondedAt.Should().BeNull();
        record.ErrorMessage.Should().BeNull();
        record.ResponsePayload.Should().BeNull();
        record.ResponseStatusCode.Should().BeNull();
    }

    [Fact]
    public void MarkClaimed_ShouldTransitionOnlyFromNotStarted()
    {
        var record = Create();
        record.MarkClaimed();
        record.Status.Should().Be(ExecutionStatus.Claimed);

        FluentActions.Invoking(record.MarkClaimed)
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Execution is not awaiting a claim.");
    }

    [Fact]
    public void MarkDispatching_ShouldTransitionFromNotStartedOrClaimed()
    {
        var notStarted = Create();
        notStarted.MarkDispatching();
        notStarted.Status.Should().Be(ExecutionStatus.Dispatching);
        notStarted.DispatchedAt.Should().NotBeNull();

        var claimed = Create();
        claimed.MarkClaimed();
        claimed.MarkDispatching();
        claimed.Status.Should().Be(ExecutionStatus.Dispatching);
        claimed.DispatchedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(ExecutionStatus.Dispatching)]
    [InlineData(ExecutionStatus.Responded)]
    [InlineData(ExecutionStatus.FailedBeforeDispatch)]
    [InlineData(ExecutionStatus.Unknown)]
    [InlineData(ExecutionStatus.CancelledBeforeDispatch)]
    public void MarkDispatching_ShouldRejectNonDispatchableStatus(ExecutionStatus status)
    {
        var record = ExecutionRecord.Restore("id", Invocation, status, null, null, null, null);
        FluentActions.Invoking(record.MarkDispatching)
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Execution is not dispatchable.");
    }

    [Fact]
    public void MarkResponded_ShouldCaptureUpstreamAndResponse()
    {
        var record = Create();
        record.MarkResponded("upstream-1", "{\"ok\":true}", 200, "warning");

        record.Status.Should().Be(ExecutionStatus.Responded);
        record.UpstreamReference.Should().Be("upstream-1");
        record.ResponsePayload.Should().Be("{\"ok\":true}");
        record.ResponseStatusCode.Should().Be(200);
        record.ErrorMessage.Should().Be("warning");
        record.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkResponded_ShouldAllowNullOptionalDetails()
    {
        var record = Create();
        record.MarkResponded(null);
        record.Status.Should().Be(ExecutionStatus.Responded);
        record.UpstreamReference.Should().BeNull();
        record.ResponsePayload.Should().BeNull();
        record.ResponseStatusCode.Should().BeNull();
        record.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkFailedBeforeDispatch_ShouldCaptureReasonAndResponseTime()
    {
        var record = Create();
        record.MarkFailedBeforeDispatch("invalid credentials");
        record.Status.Should().Be(ExecutionStatus.FailedBeforeDispatch);
        record.ErrorMessage.Should().Be("invalid credentials");
        record.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkUnknown_ShouldCaptureReasonWithoutResponseTimestamp()
    {
        var record = Create();
        record.MarkUnknown("upstream outcome is uncertain");
        record.Status.Should().Be(ExecutionStatus.Unknown);
        record.ErrorMessage.Should().Be("upstream outcome is uncertain");
        record.RespondedAt.Should().BeNull();
    }

    [Fact]
    public void MarkCancelledBeforeDispatch_ShouldAllowNotStartedOrClaimed()
    {
        var notStarted = Create();
        notStarted.MarkCancelledBeforeDispatch();
        notStarted.Status.Should().Be(ExecutionStatus.CancelledBeforeDispatch);
        notStarted.RespondedAt.Should().NotBeNull();

        var claimed = Create();
        claimed.MarkClaimed();
        claimed.MarkCancelledBeforeDispatch();
        claimed.Status.Should().Be(ExecutionStatus.CancelledBeforeDispatch);
        claimed.RespondedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(ExecutionStatus.Dispatching)]
    [InlineData(ExecutionStatus.Responded)]
    [InlineData(ExecutionStatus.Unknown)]
    public void MarkCancelledBeforeDispatch_ShouldRejectAlreadyStartedExecution(ExecutionStatus status)
    {
        var record = ExecutionRecord.Restore("id", Invocation, status, null, null, null, null);
        FluentActions.Invoking(record.MarkCancelledBeforeDispatch)
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Execution already started.");
    }

    [Fact]
    public void Restore_ShouldPreserveAllPersistedFields()
    {
        var dispatched = DateTimeOffset.Parse("2026-02-01T10:00:00Z");
        var responded = dispatched.AddSeconds(3);
        var record = ExecutionRecord.Restore("restored", Invocation, ExecutionStatus.Completed,
            "upstream", dispatched, responded, "error", "payload", 201);

        record.Id.Should().Be("restored");
        record.InvocationId.Should().Be(Invocation);
        record.Status.Should().Be(ExecutionStatus.Completed);
        record.UpstreamReference.Should().Be("upstream");
        record.DispatchedAt.Should().Be(dispatched);
        record.RespondedAt.Should().Be(responded);
        record.ErrorMessage.Should().Be("error");
        record.ResponsePayload.Should().Be("payload");
        record.ResponseStatusCode.Should().Be(201);
    }

    [Fact]
    public void ExecutionEvents_ShouldExposeConstructorData()
    {
        var now = DateTimeOffset.UtcNow;
        var dispatched = new ExecutionDispatched(Invocation, "upstream", now);
        var reconciled = new ExecutionReconciled("execution", "reviewer", "predicate-v1", now);

        dispatched.InvocationId.Should().Be(Invocation);
        dispatched.UpstreamReference.Should().Be("upstream");
        dispatched.DispatchedAt.Should().Be(now);
        reconciled.ExecutionId.Should().Be("execution");
        reconciled.ReconciledBy.Should().Be("reviewer");
        reconciled.PredicateVersion.Should().Be("predicate-v1");
        reconciled.OccurredAt.Should().Be(now);
    }

    [Fact]
    public void OutcomeEvidence_ShouldInitializeAndRestoreObservedAt()
    {
        var before = DateTimeOffset.UtcNow;
        var evidence = new OutcomeEvidence("e-1", "execution-1", OutcomeStatus.Verified,
            "monitor", "predicate-v2", "completed", "sha256:abc", "none");
        var after = DateTimeOffset.UtcNow;
        var observed = DateTimeOffset.Parse("2026-03-01T10:00:00Z");
        var restored = OutcomeEvidence.Restore("e-2", "execution-2", OutcomeStatus.Contradicted,
            null, null, null, null, "inconsistent", observed);

        evidence.Id.Should().Be("e-1");
        evidence.ExecutionId.Should().Be("execution-1");
        evidence.Status.Should().Be(OutcomeStatus.Verified);
        evidence.Source.Should().Be("monitor");
        evidence.PredicateVersion.Should().Be("predicate-v2");
        evidence.SafeSummary.Should().Be("completed");
        evidence.EvidenceDigest.Should().Be("sha256:abc");
        evidence.ContradictionReason.Should().Be("none");
        evidence.ObservedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        restored.Id.Should().Be("e-2");
        restored.ExecutionId.Should().Be("execution-2");
        restored.Status.Should().Be(OutcomeStatus.Contradicted);
        restored.Source.Should().BeNull();
        restored.PredicateVersion.Should().BeNull();
        restored.SafeSummary.Should().BeNull();
        restored.EvidenceDigest.Should().BeNull();
        restored.ContradictionReason.Should().Be("inconsistent");
        restored.ObservedAt.Should().Be(observed);
    }
}
