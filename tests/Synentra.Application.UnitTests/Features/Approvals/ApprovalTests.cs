using FluentAssertions;
using NSubstitute;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Approvals;
using Synentra.Application.Features.Approvals.Commands.ApproveInvocation;
using Synentra.Application.Features.Approvals.Commands.CreateApproval;
using Synentra.Application.Features.Approvals.Commands.RejectInvocation;
using Synentra.Application.Features.Approvals.Commands.RevokeApproval;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Application.Features.Approvals.Queries.GetApproval;
using Synentra.Application.Features.Approvals.Queries.ListPendingApprovals;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Tests.TestHelpers;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Approvals;
using Synentra.Domain.Common.Results;
using Synentra.Domain.Security;
using Void = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Tests.Features.Approvals;

public sealed class ApprovalTests
{
    private readonly IApprovalStore _store = Substitute.For<IApprovalStore>();
    private readonly IAuditStore _audit = Substitute.For<IAuditStore>();
    private readonly IAdminPolicy _adminPolicy = Substitute.For<IAdminPolicy>();

    [Fact]
    public async Task CreateApprovalCommandHandler_ShouldPersistApprovalAndWriteAuditEntry()
    {
        var handler = new CreateApprovalCommandHandler(_store, _audit);
        var invocation = TestDataBuilder.CreateInvocation();
        var cmd = new CreateApprovalCommand(invocation, "rev-1", "obl-1");

        var result = await handler.Handle(cmd, TestContext.Current.CancellationToken);

        result.InvocationId.Should().Be(invocation.Id);
        result.PolicyRevision.Should().Be("rev-1");
        result.ReviewObligationRevision.Should().Be("obl-1");
        result.Status.Should().Be(ApprovalStatus.Pending);

        await _store.Received(1).SaveAsync(result, Arg.Any<CancellationToken>());
        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditEntry>(a => a.Category == "approval-created" && a.ApprovalId == result.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveInvocationCommandHandler_WhenValidReviewer_ShouldApproveAndAudit()
    {
        _adminPolicy.AllowsCrossScopeApproval.Returns(false);
        _adminPolicy.AllowsSelfApproval.Returns(false);

        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1", securityScope: "scope-1");
        var approval = TestDataBuilder.CreateApproval("app-1", invocation);
        _store.GetAsync("app-1", Arg.Any<CancellationToken>()).Returns(approval);

        var reviewer = TestDataBuilder.CreatePrincipal("reviewer-1", "scope-1", PrincipalType.Reviewer);
        var handler = new ApproveInvocationCommandHandler(_store, _audit, _adminPolicy);

        var result = await handler.Handle(new ApproveInvocationCommand("app-1", reviewer, false), TestContext.Current.CancellationToken);

        result.Should().Be(Void.Value);
        approval.Status.Should().Be(ApprovalStatus.Approved);
        await _store.Received(1).SaveAsync(approval, Arg.Any<CancellationToken>());
        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditEntry>(a => a.Category == "approval-approved" && a.ActorId == "reviewer-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveInvocationCommandHandler_WhenApprovalNotFound_ShouldThrowSynentraApplicationException()
    {
        _store.GetAsync("missing", Arg.Any<CancellationToken>()).Returns((Approval?)null);
        var reviewer = TestDataBuilder.CreatePrincipal("reviewer-1", "scope-1", PrincipalType.Reviewer);
        var handler = new ApproveInvocationCommandHandler(_store, _audit, _adminPolicy);

        var act = () => handler.Handle(new ApproveInvocationCommand("missing", reviewer, false), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<SynentraApplicationException>();
    }

    [Fact]
    public async Task ApproveInvocationCommandHandler_WhenPrincipalIsAgent_ShouldThrowSynentraApplicationException()
    {
        var approval = TestDataBuilder.CreateApproval("app-1");
        _store.GetAsync("app-1", Arg.Any<CancellationToken>()).Returns(approval);

        var agent = TestDataBuilder.CreatePrincipal("agent-2", "scope-default", PrincipalType.Agent);
        var handler = new ApproveInvocationCommandHandler(_store, _audit, _adminPolicy);

        var act = () => handler.Handle(new ApproveInvocationCommand("app-1", agent, false), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<SynentraApplicationException>();
    }

    [Fact]
    public async Task ApproveInvocationCommandHandler_WhenSelfApprovalWithoutPolicyPermission_ShouldThrow()
    {
        _adminPolicy.AllowsCrossScopeApproval.Returns(false);
        _adminPolicy.AllowsSelfApproval.Returns(false);

        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1", securityScope: "scope-1");
        var approval = TestDataBuilder.CreateApproval("app-1", invocation);
        _store.GetAsync("app-1", Arg.Any<CancellationToken>()).Returns(approval);

        var selfReviewer = TestDataBuilder.CreatePrincipal("agent-1", "scope-1", PrincipalType.Admin);
        var handler = new ApproveInvocationCommandHandler(_store, _audit, _adminPolicy);

        var act = () => handler.Handle(new ApproveInvocationCommand("app-1", selfReviewer, true), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<SynentraApplicationException>();
    }

    [Fact]
    public async Task ApproveInvocationCommandHandler_WhenSelfApprovalAllowedByCommandAndPolicy_ShouldSucceed()
    {
        _adminPolicy.AllowsCrossScopeApproval.Returns(false);
        _adminPolicy.AllowsSelfApproval.Returns(true);

        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1", securityScope: "scope-1");
        var approval = TestDataBuilder.CreateApproval("app-1", invocation);
        _store.GetAsync("app-1", Arg.Any<CancellationToken>()).Returns(approval);

        var selfReviewer = TestDataBuilder.CreatePrincipal("agent-1", "scope-1", PrincipalType.Admin);
        var handler = new ApproveInvocationCommandHandler(_store, _audit, _adminPolicy);

        await handler.Handle(new ApproveInvocationCommand("app-1", selfReviewer, true), TestContext.Current.CancellationToken);

        approval.Status.Should().Be(ApprovalStatus.Approved);
    }

    [Fact]
    public async Task RejectInvocationCommandHandler_WhenValidReviewer_ShouldRejectAndAuditReason()
    {
        _adminPolicy.AllowsCrossScopeApproval.Returns(false);
        _adminPolicy.AllowsSelfApproval.Returns(false);

        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1", securityScope: "scope-1");
        var approval = TestDataBuilder.CreateApproval("app-1", invocation);
        _store.GetAsync("app-1", Arg.Any<CancellationToken>()).Returns(approval);

        var reviewer = TestDataBuilder.CreatePrincipal("reviewer-1", "scope-1", PrincipalType.Reviewer);
        var handler = new RejectInvocationCommandHandler(_store, _audit, _adminPolicy);

        await handler.Handle(new RejectInvocationCommand("app-1", reviewer, "High risk"), TestContext.Current.CancellationToken);

        approval.Status.Should().Be(ApprovalStatus.Rejected);
        await _store.Received(1).SaveAsync(approval, Arg.Any<CancellationToken>());
        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditEntry>(a => a.Category == "approval-rejected" && a.Details["reason"] == "High risk"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectInvocationCommandHandler_WhenCrossScopeDisallowed_ShouldThrow()
    {
        _adminPolicy.AllowsCrossScopeApproval.Returns(false);
        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1", securityScope: "scope-1");
        var approval = TestDataBuilder.CreateApproval("app-1", invocation);
        _store.GetAsync("app-1", Arg.Any<CancellationToken>()).Returns(approval);

        var reviewer = TestDataBuilder.CreatePrincipal("reviewer-1", "scope-other", PrincipalType.Reviewer);
        var handler = new RejectInvocationCommandHandler(_store, _audit, _adminPolicy);

        var act = () => handler.Handle(new RejectInvocationCommand("app-1", reviewer, "reason"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<SynentraApplicationException>();
    }

    [Fact]
    public async Task RevokeApprovalCommandHandler_WhenRequestingAgentOrAdmin_ShouldRevoke()
    {
        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1");
        var approval = TestDataBuilder.CreateApproval("app-1", invocation);
        _store.GetAsync("app-1", Arg.Any<CancellationToken>()).Returns(approval);

        var handler = new RevokeApprovalCommandHandler(_store);
        var requester = TestDataBuilder.CreatePrincipal("agent-1", type: PrincipalType.Agent);

        await handler.Handle(new RevokeApprovalCommand("app-1", requester), TestContext.Current.CancellationToken);

        approval.Status.Should().Be(ApprovalStatus.Revoked);
        await _store.Received(1).SaveAsync(approval, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeApprovalCommandHandler_WhenUnauthorizedAgent_ShouldThrow()
    {
        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1");
        var approval = TestDataBuilder.CreateApproval("app-1", invocation);
        _store.GetAsync("app-1", Arg.Any<CancellationToken>()).Returns(approval);

        var handler = new RevokeApprovalCommandHandler(_store);
        var otherAgent = TestDataBuilder.CreatePrincipal("agent-2", type: PrincipalType.Agent);

        var act = () => handler.Handle(new RevokeApprovalCommand("app-1", otherAgent), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<SynentraApplicationException>();
    }

    [Fact]
    public async Task QueriesAndServiceAndDispatcherExtensions_ShouldDelegateProperly()
    {
        var approval = TestDataBuilder.CreateApproval("app-1");
        _store.GetAsync("app-1", Arg.Any<CancellationToken>()).Returns(approval);
        var getHandler = new GetApprovalQueryHandler(_store);
        (await getHandler.Handle(new GetApprovalQuery("app-1"), TestContext.Current.CancellationToken)).Should().BeSameAs(approval);

        var pageRequest = new PageRequest(1, 10);
        _store.ListPendingAsync("scope-1", pageRequest, Arg.Any<CancellationToken>())
            .Returns(new Page<Approval>(new[] { approval }, 1));
        var listHandler = new ListPendingApprovalsQueryHandler(_store);
        var paged = await listHandler.Handle(new ListPendingApprovalsQuery("scope-1", pageRequest), TestContext.Current.CancellationToken);
        paged.Items.Should().ContainSingle().Which.Should().BeSameAs(approval);
        paged.TotalCount.Should().Be(1);

        // Service & Dispatcher Extensions
        var dispatcher = Substitute.For<IDispatcher>();
        var service = new ApprovalService(dispatcher);
        var inv = TestDataBuilder.CreateInvocation();
        var principal = TestDataBuilder.CreatePrincipal(type: PrincipalType.Admin);

        await service.CreateAsync(inv, "r1", "o1", TestContext.Current.CancellationToken);
        await service.ApproveAsync("app-1", principal, false, TestContext.Current.CancellationToken);
        await service.RejectAsync("app-1", principal, "no", TestContext.Current.CancellationToken);
        await service.RevokeAsync("app-1", principal, TestContext.Current.CancellationToken);

        await dispatcher.GetApproval("app-1", TestContext.Current.CancellationToken);
        await dispatcher.ListPendingApprovals("scope-1", null, null, TestContext.Current.CancellationToken);
        await dispatcher.Approve("app-1", principal, true, TestContext.Current.CancellationToken);
        await dispatcher.Reject("app-1", principal, "reason", TestContext.Current.CancellationToken);
        await dispatcher.Revoke("app-1", principal, TestContext.Current.CancellationToken);

        await dispatcher.Received(1).Dispatch(Arg.Any<CreateApprovalCommand>(), Arg.Any<CancellationToken>());
        await dispatcher.Received(2).Dispatch(Arg.Any<ApproveInvocationCommand>(), Arg.Any<CancellationToken>());
        await dispatcher.Received(2).Dispatch(Arg.Any<RejectInvocationCommand>(), Arg.Any<CancellationToken>());
        await dispatcher.Received(2).Dispatch(Arg.Any<RevokeApprovalCommand>(), Arg.Any<CancellationToken>());
        await dispatcher.Received(1).Dispatch(Arg.Any<GetApprovalQuery>(), Arg.Any<CancellationToken>());
        await dispatcher.Received(1).Dispatch(Arg.Is<ListPendingApprovalsQuery>(q => q.PageRequest.PageNumber == 1 && q.PageRequest.PageSize == 20), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validators_ShouldValidateRequiredFields()
    {
        var principal = TestDataBuilder.CreatePrincipal();

        new ApproveInvocationCommandValidator().Validate(new ApproveInvocationCommand("", principal, false)).IsValid.Should().BeFalse();
        new ApproveInvocationCommandValidator().Validate(new ApproveInvocationCommand("id", null!, false)).IsValid.Should().BeFalse();
        new ApproveInvocationCommandValidator().Validate(new ApproveInvocationCommand("id", principal, false)).IsValid.Should().BeTrue();

        new CreateApprovalCommandValidator().Validate(new CreateApprovalCommand(null!, "r1", "o1")).IsValid.Should().BeFalse();
        new CreateApprovalCommandValidator().Validate(new CreateApprovalCommand(TestDataBuilder.CreateInvocation(), "", "o1")).IsValid.Should().BeFalse();
        new CreateApprovalCommandValidator().Validate(new CreateApprovalCommand(TestDataBuilder.CreateInvocation(), "r1", "")).IsValid.Should().BeFalse();
        new CreateApprovalCommandValidator().Validate(new CreateApprovalCommand(TestDataBuilder.CreateInvocation(), "r1", "o1")).IsValid.Should().BeTrue();

        new RejectInvocationCommandValidator().Validate(new RejectInvocationCommand("", principal, null)).IsValid.Should().BeFalse();
        new RejectInvocationCommandValidator().Validate(new RejectInvocationCommand("id", null!, null)).IsValid.Should().BeFalse();
        new RejectInvocationCommandValidator().Validate(new RejectInvocationCommand("id", principal, null)).IsValid.Should().BeTrue();

        new RevokeApprovalCommandValidator().Validate(new RevokeApprovalCommand("", principal)).IsValid.Should().BeFalse();
        new RevokeApprovalCommandValidator().Validate(new RevokeApprovalCommand("id", null!)).IsValid.Should().BeFalse();
        new RevokeApprovalCommandValidator().Validate(new RevokeApprovalCommand("id", principal)).IsValid.Should().BeTrue();

        new GetApprovalQueryValidator().Validate(new GetApprovalQuery("")).IsValid.Should().BeFalse();
        new GetApprovalQueryValidator().Validate(new GetApprovalQuery("id")).IsValid.Should().BeTrue();

        new ListPendingApprovalsQueryValidator().Validate(new ListPendingApprovalsQuery("", new PageRequest(1, 10))).IsValid.Should().BeFalse();
        new ListPendingApprovalsQueryValidator().Validate(new ListPendingApprovalsQuery("scope", new PageRequest(0, 10))).IsValid.Should().BeTrue();
        new ListPendingApprovalsQueryValidator().Validate(new ListPendingApprovalsQuery("scope", new PageRequest(1, 0))).IsValid.Should().BeTrue();
        new ListPendingApprovalsQueryValidator().Validate(new ListPendingApprovalsQuery("scope", new PageRequest(1, 10))).IsValid.Should().BeTrue();
    }
}
