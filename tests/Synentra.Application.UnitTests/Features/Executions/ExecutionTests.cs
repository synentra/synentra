using FluentAssertions;
using NSubstitute;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Executions;
using Synentra.Application.Features.Executions.Commands.CancelExecution;
using Synentra.Application.Features.Executions.Commands.ReconcileExecution;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Executions.Queries.GetExecution;
using Synentra.Application.Features.Executions.Reconciliation;
using Synentra.Application.Features.Invocations.Admission;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Tests.TestHelpers;
using Synentra.Domain.Execution;
using Synentra.Domain.Execution.Evidence;
using Synentra.Domain.Security;

namespace Synentra.Application.Tests.Features.Executions;

public sealed class ExecutionTests
{
    private readonly IExecutionRecordStore _records = Substitute.For<IExecutionRecordStore>();
    private readonly IInvocationStore _invocations = Substitute.For<IInvocationStore>();
    private readonly IExecutionClaimStore _claims = Substitute.For<IExecutionClaimStore>();
    private readonly IOutcomeVerifier _verifier = Substitute.For<IOutcomeVerifier>();
    private readonly IAuditStore _audit = Substitute.For<IAuditStore>();
    private readonly IOutcomeEvidenceStore _evidence = Substitute.For<IOutcomeEvidenceStore>();

    [Fact]
    public async Task CancelExecutionCommandHandler_WhenClaimedAndAuthorized_ShouldCancelBeforeDispatch()
    {
        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1", securityScope: "scope-1");
        var record = TestDataBuilder.CreateExecutionRecord("exec-1", invocation.Id.ToString());
        record.MarkClaimed();

        _records.GetAsync("exec-1", Arg.Any<CancellationToken>()).Returns(record);
        _invocations.GetAsync(invocation.Id.ToString(), Arg.Any<CancellationToken>()).Returns(invocation);
        _claims.CancelBeforeDispatchAsync(invocation.Id.ToString(), Arg.Any<CancellationToken>()).Returns(true);

        var caller = TestDataBuilder.CreatePrincipal("agent-1", "scope-1", PrincipalType.Agent);
        var handler = new CancelExecutionCommandHandler(_records, _invocations, _claims);

        var cancelled = await handler.Handle(new CancelExecutionCommand("exec-1", caller), TestContext.Current.CancellationToken);

        cancelled.Should().BeTrue();
        record.Status.Should().Be(ExecutionStatus.CancelledBeforeDispatch);
        await _records.Received(1).SaveAsync(record, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelExecutionCommandHandler_WhenUnauthorizedCaller_ShouldThrowUnauthorizedAccessException()
    {
        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1", securityScope: "scope-1");
        var record = TestDataBuilder.CreateExecutionRecord("exec-1", invocation.Id.ToString());

        _records.GetAsync("exec-1", Arg.Any<CancellationToken>()).Returns(record);
        _invocations.GetAsync(invocation.Id.ToString(), Arg.Any<CancellationToken>()).Returns(invocation);

        var otherAgent = TestDataBuilder.CreatePrincipal("agent-2", "scope-1", PrincipalType.Agent);
        var handler = new CancelExecutionCommandHandler(_records, _invocations, _claims);

        var act = () => handler.Handle(new CancelExecutionCommand("exec-1", otherAgent), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CancelExecutionCommandHandler_WhenAlreadyDispatchingOrClaimCannotCancel_ShouldReturnFalse()
    {
        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1", securityScope: "scope-1");
        var record = TestDataBuilder.CreateExecutionRecord("exec-1", invocation.Id.ToString());
        record.MarkClaimed();
        record.MarkDispatching();

        _records.GetAsync("exec-1", Arg.Any<CancellationToken>()).Returns(record);
        _invocations.GetAsync(invocation.Id.ToString(), Arg.Any<CancellationToken>()).Returns(invocation);

        var admin = TestDataBuilder.CreatePrincipal("admin-1", "scope-other", PrincipalType.Admin);
        var handler = new CancelExecutionCommandHandler(_records, _invocations, _claims);

        var result = await handler.Handle(new CancelExecutionCommand("exec-1", admin), TestContext.Current.CancellationToken);

        result.Should().BeFalse();
        await _claims.DidNotReceiveWithAnyArgs().CancelBeforeDispatchAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CancelExecutionCommandHandler_WhenExecutionMissing_ShouldThrowKeyNotFoundException()
    {
        _records.GetAsync("missing", Arg.Any<CancellationToken>()).Returns((ExecutionRecord?)null);
        var caller = TestDataBuilder.CreatePrincipal(type: PrincipalType.Admin);
        var handler = new CancelExecutionCommandHandler(_records, _invocations, _claims);

        var act = () => handler.Handle(new CancelExecutionCommand("missing", caller));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ReconcileExecutionCommandHandler_WhenValidAdminInScope_ShouldVerifyAppendEvidenceAndAudit()
    {
        var invocation = TestDataBuilder.CreateInvocation(securityScope: "scope-1");
        var record = TestDataBuilder.CreateExecutionRecord("exec-1", invocation.Id.ToString());
        var reg = new VerifierRegistration("order-verifier", "1", new Dictionary<string, string>());
        var expectedEvidence = new OutcomeEvidence("evidence-1", "exec-1", OutcomeStatus.Verified, "order-verifier", "1", "verified", "ref-1");

        _records.GetAsync("exec-1", Arg.Any<CancellationToken>()).Returns(record);
        _invocations.GetAsync(invocation.Id.ToString(), Arg.Any<CancellationToken>()).Returns(invocation);
        _verifier.VerifierId.Returns("order-verifier");
        _verifier.Version.Returns("1");
        _verifier.VerifyAsync(record, reg, Arg.Any<CancellationToken>()).Returns(expectedEvidence);

        var admin = TestDataBuilder.CreatePrincipal("admin-1", "scope-1", PrincipalType.Admin);
        var handler = new ReconcileExecutionCommandHandler(_records, _verifier, _audit, _evidence, _invocations);

        var result = await handler.Handle(new ReconcileExecutionCommand("exec-1", reg, admin), TestContext.Current.CancellationToken);

        result.Should().Be(expectedEvidence);
        await _evidence.Received(1).AppendAsync("exec-1", expectedEvidence, Arg.Any<CancellationToken>());
        await _audit.Received(1).AppendAsync(Arg.Is<AuditEntry>(a => a.Category == "reconciliation"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReconcileExecutionCommandHandler_WhenNonAdminOrWrongScopeOrVerifierMismatch_ShouldThrow()
    {
        var invocation = TestDataBuilder.CreateInvocation(securityScope: "scope-1");
        var record = TestDataBuilder.CreateExecutionRecord("exec-1", invocation.Id.ToString());
        var reg = new VerifierRegistration("order-verifier", "1", new Dictionary<string, string>());

        _records.GetAsync("exec-1", Arg.Any<CancellationToken>()).Returns(record);
        _invocations.GetAsync(invocation.Id.ToString(), Arg.Any<CancellationToken>()).Returns(invocation);
        _verifier.VerifierId.Returns("order-verifier");
        _verifier.Version.Returns("2");

        var handler = new ReconcileExecutionCommandHandler(_records, _verifier, _audit, _evidence, _invocations);
        var wrongScopeAdmin = TestDataBuilder.CreatePrincipal("admin-1", "scope-2", PrincipalType.Admin);
        var matchingAdmin = TestDataBuilder.CreatePrincipal("admin-1", "scope-1", PrincipalType.Admin);

        await FluentActions.Invoking(() => handler.Handle(new ReconcileExecutionCommand("exec-1", reg, wrongScopeAdmin)))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        await FluentActions.Invoking(() => handler.Handle(new ReconcileExecutionCommand("exec-1", reg, matchingAdmin)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*verifier*");
    }

    [Fact]
    public void AuthorizedHttpTarget_ShouldValidateAndMatchExactRequestUri()
    {
        var validTarget = new AuthorizedHttpTarget(
            "target-1",
            new Uri("https://api.example.com"),
            new Uri("https://api.example.com/v1/items?limit=10"),
            "rev-1");

        validTarget.IsValid.Should().BeTrue();
        validTarget.Matches(new Uri("https://api.example.com/v1/items?limit=10")).Should().BeTrue();
        validTarget.Matches(new Uri("https://api.example.com/v1/items?limit=20")).Should().BeFalse();
        validTarget.Matches(new Uri("https://evil.example.com/v1/items?limit=10")).Should().BeFalse();

        var mismatchedHost = new AuthorizedHttpTarget(
            "target-1",
            new Uri("https://api.example.com"),
            new Uri("https://other.example.com/v1/items"),
            "rev-1");
        mismatchedHost.IsValid.Should().BeFalse();

        var withUserInfo = new AuthorizedHttpTarget(
            "target-1",
            new Uri("https://user:pass@api.example.com"),
            new Uri("https://user:pass@api.example.com/v1/items"),
            "rev-1");
        withUserInfo.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task QueriesServicesValidatorsAndExtensions_ShouldWorkAsExpected()
    {
        var record = TestDataBuilder.CreateExecutionRecord("exec-1");
        _records.GetAsync("exec-1", Arg.Any<CancellationToken>()).Returns(record);

        var getHandler = new GetExecutionQueryHandler(_records);
        (await getHandler.Handle(new GetExecutionQuery("exec-1"), TestContext.Current.CancellationToken)).Should().BeSameAs(record);

        new GetExecutionQueryValidator().Validate(new GetExecutionQuery("")).IsValid.Should().BeFalse();
        new GetExecutionQueryValidator().Validate(new GetExecutionQuery("exec-1")).IsValid.Should().BeTrue();

        var reg = new VerifierRegistration("v", "1", new Dictionary<string, string>());
        var admin = TestDataBuilder.CreatePrincipal(type: PrincipalType.Admin);
        new ReconcileExecutionCommandValidator().Validate(new ReconcileExecutionCommand("", reg, admin)).IsValid.Should().BeFalse();
        new ReconcileExecutionCommandValidator().Validate(new ReconcileExecutionCommand("exec-1", reg, admin)).IsValid.Should().BeTrue();

        var dispatcher = Substitute.For<IDispatcher>();
        var reconciliationService = new ReconciliationService(dispatcher);
        await reconciliationService.ReconcileAsync("exec-1", reg, admin, TestContext.Current.CancellationToken);
        await dispatcher.GetExecutions("exec-1", TestContext.Current.CancellationToken);
        await dispatcher.Reconcile("exec-1", reg, admin, TestContext.Current.CancellationToken);
        await dispatcher.Cancel("exec-1", admin, TestContext.Current.CancellationToken);

        await dispatcher.Received(2).Dispatch(Arg.Any<ReconcileExecutionCommand>(), Arg.Any<CancellationToken>());
        await dispatcher.Received(1).Dispatch(Arg.Any<GetExecutionQuery>(), Arg.Any<CancellationToken>());
        await dispatcher.Received(1).Dispatch(Arg.Any<CancelExecutionCommand>(), Arg.Any<CancellationToken>());

        var unknownReceipt = TransportReceipt.Unknown("timeout");
        unknownReceipt.Status.Should().Be(ReceiptStatus.Unknown);
        unknownReceipt.ErrorMessage.Should().Be("timeout");
    }
}
