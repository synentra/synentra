using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Messaging;
using Synentra.Application.Common.Persistence;
using Synentra.Application.Common.Serialization;
using Synentra.Application.Features.Approvals;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Executions.Messages;
using Synentra.Application.Features.Invocations.Admission;
using Synentra.Application.Features.Invocations.Commands.ResumeInvocation;
using Synentra.Application.Features.Invocations.Commands.SimulateInvocation;
using Synentra.Application.Features.Invocations.Commands.SubmitInvocation;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Invocations.Evaluation;
using Synentra.Application.Features.Invocations.Exceptions;
using Synentra.Application.Features.Invocations.Normalization;
using Synentra.Application.Features.Invocations.Queries.GetInvocation;
using Synentra.Application.Features.Invocations.Queries.GetInvocationResult;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Application.Features.Trust.Contracts;
using Synentra.Application.Tests.TestHelpers;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Execution;
using Synentra.Domain.Execution.Evidence;
using Synentra.Domain.Approvals;
using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Trust.ValueObjects;
using Synentra.Domain.Governance.Trust;
using Synentra.Domain.Invocations;
using Synentra.Domain.Resources.ValueObjects;
using Synentra.Domain.Security;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Tests.Features.Invocations;

public sealed class AdmissionAndResumeAndSubmitTests
{
    private readonly IGrantStore _grants = Substitute.For<IGrantStore>();
    private readonly IApprovalStore _approvals = Substitute.For<IApprovalStore>();
    private readonly IExecutionClaimStore _claims = Substitute.For<IExecutionClaimStore>();
    private readonly IExecutionRecordStore _records = Substitute.For<IExecutionRecordStore>();
    private readonly IInvocationStore _invocations = Substitute.For<IInvocationStore>();
    private readonly IAuditStore _audit = Substitute.For<IAuditStore>();
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();
    private readonly ITrustProvider _trust = Substitute.For<ITrustProvider>();
    private readonly ILogger<AdmissionService> _admissionLogger = Substitute.For<ILogger<AdmissionService>>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IExecutionSnapshotFactory _snapshots = Substitute.For<IExecutionSnapshotFactory>();
    private readonly IRevisionProvider _revisions = Substitute.For<IRevisionProvider>();

    public AdmissionAndResumeAndSubmitTests()
    {
        _uow.ExecuteAsync(Arg.Any<Func<CancellationToken, Task<AdmissionResult>>>(), Arg.Any<Func<AdmissionResult, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task<AdmissionResult>>>()(ci.Arg<CancellationToken>()));
    }

    private AdmissionService CreateAdmissionService() => new(
        _grants, _approvals, _claims, _records, _invocations, _audit, _outbox, _trust, _admissionLogger, _uow, _snapshots, _revisions);

    [Fact]
    public async Task AdmissionService_WhenDecisionIsNotAllow_ShouldReturnNotAdmitted()
    {
        var sut = CreateAdmissionService();
        var denyDecision = ExecutionDecision.Deny(new[] { "DENIED" });

        var res = await sut.AdmitAsync(denyDecision, TestDataBuilder.CreateInvocation(), TestContext.Current.CancellationToken);

        res.Admitted.Should().BeFalse();
        res.Decision.Should().Be(denyDecision);
    }

    [Fact]
    public async Task AdmissionService_WhenManifestRevisionChanged_ShouldDenyWithManifestChanged()
    {
        _revisions.ActiveRevision.Returns("rev-2");
        var decision = TestDataBuilder.CreateAllowDecision(manifestRevision: "rev-1");
        var sut = CreateAdmissionService();

        var res = await sut.AdmitAsync(decision, TestDataBuilder.CreateInvocation(), TestContext.Current.CancellationToken);

        res.Admitted.Should().BeFalse();
        res.Decision.ReasonCodes.Should().Contain("DECISION_EXPIRED_OR_MANIFEST_CHANGED");
    }

    [Fact]
    public async Task AdmissionService_WhenRequiresGrantAndMissingOrInvalid_ShouldDeny()
    {
        _revisions.ActiveRevision.Returns("rev-1");
        _trust.LoadSnapshotAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TrustSnapshot(new TrustScore(0.9), TrustStatus.Normal, "scope-default", false, Array.Empty<TrustFactor>(), DateTimeOffset.UtcNow));

        var decision = TestDataBuilder.CreateAllowDecision(manifestRevision: "rev-1");
        var invNoGrant = TestDataBuilder.CreateInvocation(requiresGrant: true, taskGrantId: null);
        var sut = CreateAdmissionService();

        var resMissing = await sut.AdmitAsync(decision, invNoGrant, TestContext.Current.CancellationToken);
        resMissing.Admitted.Should().BeFalse();
        resMissing.Decision.ReasonCodes.Should().Contain("TASK_GRANT_REQUIRED");

        var wrongOpGrant = TestDataBuilder.CreateTaskGrant(id: "g-1", agentId: "agent-1", operationIds: new[] { "other.op" });
        _grants.GetAsync("g-1", Arg.Any<CancellationToken>()).Returns(wrongOpGrant);
        var invWrongOp = TestDataBuilder.CreateInvocation(requiresGrant: true, taskGrantId: "g-1", operationId: "orders.create");

        var resMismatch = await sut.AdmitAsync(decision, invWrongOp, TestContext.Current.CancellationToken);
        resMismatch.Admitted.Should().BeFalse();
        resMismatch.Decision.ReasonCodes.Should().Contain("TASK_GRANT_OPERATION_MISMATCH");
    }

    [Fact]
    public async Task AdmissionService_WhenValidAndClaimAcquired_ShouldPersistRecordAuditAndEnqueueDispatch()
    {
        _revisions.ActiveRevision.Returns("rev-1");
        _trust.LoadSnapshotAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TrustSnapshot(new TrustScore(0.9), TrustStatus.Normal, "scope-default", false, Array.Empty<TrustFactor>(), DateTimeOffset.UtcNow));

        var grant = TestDataBuilder.CreateTaskGrant(id: "g-1", agentId: "agent-1", operationIds: new[] { "orders.create" });
        _grants.GetAsync("g-1", Arg.Any<CancellationToken>()).Returns(grant);

        var invocation = TestDataBuilder.CreateInvocation(requiresGrant: true, taskGrantId: "g-1");
        var decision = TestDataBuilder.CreateAllowDecision(manifestRevision: "rev-1");

        _claims.TryAcquireAsync(invocation.Id.ToString(), "agent-1", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("claim-123");
        _snapshots.CreateAsync(invocation, Arg.Any<IReadOnlyList<ResourceDescriptor>>(), Arg.Any<CancellationToken>())
            .Returns(TestDataBuilder.CreateAdmittedSnapshot(invocation));

        var sut = CreateAdmissionService();
        var res = await sut.AdmitAsync(decision, invocation, CancellationToken.None);

        res.Admitted.Should().BeTrue();
        res.ClaimId.Should().Be("claim-123");

        await _invocations.Received(1).SaveAsync(invocation, Arg.Any<CancellationToken>());
        await _records.Received(1).SaveAsync(Arg.Is<ExecutionRecord>(r => r.Status == ExecutionStatus.Claimed), Arg.Any<CancellationToken>());
        await _audit.Received(1).AppendAsync(Arg.Is<AuditEntry>(a => a.Category == "admission"), Arg.Any<CancellationToken>());
        await _outbox.Received(1).EnqueueAsync(Arg.Is<DispatchCommand>(d => d.ClaimId == "claim-123" && d.ManifestRevision == "rev-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvocationNormalizer_ShouldDelegateToMatchingModalityAndRecomputeCanonicalDigest()
    {
        var innerNormalizer = Substitute.For<IInvocationNormalizer>();
        innerNormalizer.Modality.Returns(IntentModality.Http);
        var rawInvocation = TestDataBuilder.CreateInvocation(digest: new string('d', 64));
        innerNormalizer.NormalizeAsync(Arg.Any<NormalizationInput>(), Arg.Any<CanonicalPrincipal>(), Arg.Any<CancellationToken>())
            .Returns(rawInvocation);

        var manifests = Substitute.For<IManifestProvider>();
        var registry = Substitute.For<IOperationRegistry>();
        var canonical = new CanonicalJsonService();

        var sut = new InvocationNormalizer(new[] { innerNormalizer }, manifests, canonical, registry);
        var input = new NormalizationInput("orders.create", "orders-http", null, new Dictionary<string, object>(), new Dictionary<string, string>(), new Dictionary<string, string>(), "k1", null);
        var normalized = await sut.NormalizeAsync(IntentModality.Http, input, TestDataBuilder.CreatePrincipal(), TestContext.Current.CancellationToken);

        normalized.Digest.Value.Should().NotBe("STALE_DIGEST");
        normalized.Digest.Should().Be(canonical.ComputeDigest(rawInvocation));
    }

    [Fact]
    public async Task ResumeInvocationCommandHandler_WhenApprovedAndMatchingDigest_ShouldConsumeApprovalAndAdmit()
    {
        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1", securityScope: "scope-default");
        var approval = TestDataBuilder.CreateApproval("app-1", invocation, policyRevision: "policy-v1");
        approval.Approve("reviewer-1");

        var evaluator = Substitute.For<IInvocationEvaluator>();
        var admission = Substitute.For<IExecutionAdmission>();

        _invocations.GetAsync(invocation.Id.ToString(), Arg.Any<CancellationToken>()).Returns(invocation);
        _approvals.FindByInvocationAsync(invocation.Id.ToString(), Arg.Any<CancellationToken>()).Returns(approval);

        var reviewDecision = ExecutionDecision.Review(new[] { "NEEDS_APPROVAL" }, policyRevision: "policy-v1");
        evaluator.EvaluateAsync(invocation, Arg.Any<CancellationToken>()).Returns(reviewDecision);

        var expectedAdmission = AdmissionResult.CreateAdmitted("claim-9", ExecutionDecision.Allow(reviewDecision.ReasonCodes, reviewDecision.Evidence, "policy-v1"));
        admission.AdmitAsync(Arg.Is<ExecutionDecision>(d => d.Outcome == DecisionOutcome.Allow), invocation, Arg.Any<CancellationToken>())
            .Returns(expectedAdmission);

        var handler = new ResumeInvocationCommandHandler(_approvals, _invocations, admission, evaluator);
        var res = await handler.Handle(new ResumeInvocationCommand(invocation.Id.ToString(), TestDataBuilder.CreatePrincipal("agent-1")), TestContext.Current.CancellationToken);

        res.Admitted.Should().BeTrue();
        res.ClaimId.Should().Be("claim-9");
        await _approvals.Received(1).SaveAsync(approval, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeInvocationCommandHandler_WhenCrossAgentOrExpired_ShouldThrow()
    {
        var invocation = TestDataBuilder.CreateInvocation(authenticatedAgentId: "agent-1");
        _invocations.GetAsync(invocation.Id.ToString(), Arg.Any<CancellationToken>()).Returns(invocation);

        var handler = new ResumeInvocationCommandHandler(_approvals, _invocations, Substitute.For<IExecutionAdmission>(), Substitute.For<IInvocationEvaluator>());

        await FluentActions.Invoking(() => handler.Handle(new ResumeInvocationCommand(invocation.Id.ToString(), TestDataBuilder.CreatePrincipal("other-agent"))))
            .Should().ThrowAsync<SynentraApplicationException>();

        var expiredApproval = Approval.Restore(
            "app-exp", invocation.Id, invocation.Digest, invocation.AuthenticatedAgentId,
            invocation.OperationId.Value, invocation.BindingId, invocation.TaskGrantId,
            "policy-v1", "obl-v1", invocation.SecurityScope, DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(-2), ApprovalStatus.Approved, "rev-1", DateTimeOffset.UtcNow.AddMinutes(-2));
        _approvals.FindByInvocationAsync(invocation.Id.ToString(), Arg.Any<CancellationToken>()).Returns(expiredApproval);

        await FluentActions.Invoking(() => handler.Handle(new ResumeInvocationCommand(invocation.Id.ToString(), TestDataBuilder.CreatePrincipal("agent-1"))))
            .Should().ThrowAsync<AuthorityExpiredException>();
    }

    [Fact]
    public async Task SubmitInvocationCommandHandler_WhenMutationMissingIdempotencyKey_ShouldThrow()
    {
        var innerNormalizer = Substitute.For<IInvocationNormalizer>();
        innerNormalizer.Modality.Returns(IntentModality.Http);
        var mutationInv = TestDataBuilder.CreateInvocation(isMutation: true, idempotencyKey: null);
        innerNormalizer.NormalizeAsync(Arg.Any<NormalizationInput>(), Arg.Any<CanonicalPrincipal>(), Arg.Any<CancellationToken>())
            .Returns(mutationInv);

        var canonical = new CanonicalJsonService();
        var normalizer = new InvocationNormalizer(new[] { innerNormalizer }, Substitute.For<IManifestProvider>(), canonical, Substitute.For<IOperationRegistry>());
        var handler = new SubmitInvocationCommandHandler(
            normalizer,
            Substitute.For<IInvocationEvaluator>(),
            Substitute.For<IExecutionAdmission>(),
            _invocations,
            Substitute.For<IIdempotencyStore>(),
            _approvals,
            _records,
            Substitute.For<IOutcomeEvidenceStore>(),
            new ApprovalService(Substitute.For<IDispatcher>()),
            Substitute.For<IManifestProvider>(),
            _revisions,
            canonical);

        var req = TestDataBuilder.CreateInvocationRequest(bindingId: "orders-http");
        var act = () => handler.Handle(new SubmitInvocationCommand(req, TestDataBuilder.CreatePrincipal(), null));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IDEMPOTENCY_KEY_REQUIRED");
    }

    [Fact]
    public async Task SubmitInvocationCommandHandler_WhenDuplicateIdempotencyKeyWithSameInput_ShouldReturnExistingResult()
    {
        var innerNormalizer = Substitute.For<IInvocationNormalizer>();
        innerNormalizer.Modality.Returns(IntentModality.Http);
        var inv = TestDataBuilder.CreateInvocation(id: "inv-existing", isMutation: true, idempotencyKey: "idem-1");
        innerNormalizer.NormalizeAsync(Arg.Any<NormalizationInput>(), Arg.Any<CanonicalPrincipal>(), Arg.Any<CancellationToken>())
            .Returns(inv);

        var canonical = new CanonicalJsonService();
        var normalizer = new InvocationNormalizer(new[] { innerNormalizer }, Substitute.For<IManifestProvider>(), canonical, Substitute.For<IOperationRegistry>());
        var idempotencyStore = Substitute.For<IIdempotencyStore>();

        // Compute expected input digest via a first run or matching digest
        var argDigest = canonical.CanonicalizeArgumentDigest(inv.Arguments);
        var expectedInputDigest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"v2||{argDigest}")));

        idempotencyStore.GetAsync(inv.SecurityScope, inv.AuthenticatedAgentId, inv.OperationId.Value, "idem-1", Arg.Any<CancellationToken>())
            .Returns(new IdempotencyEntry("e1", inv.SecurityScope, inv.AuthenticatedAgentId, inv.OperationId.Value, "idem-1", expectedInputDigest, inv.Digest.Value, "inv-existing", DateTimeOffset.UtcNow.AddHours(1)));

        _invocations.GetDecisionAsync("inv-existing", Arg.Any<CancellationToken>())
            .Returns(new RecordedDecision("allow", new[] { "OK" }, "policy-v1", DateTimeOffset.UtcNow.AddMinutes(5)));

        var handler = new SubmitInvocationCommandHandler(
            normalizer,
            Substitute.For<IInvocationEvaluator>(),
            Substitute.For<IExecutionAdmission>(),
            _invocations,
            idempotencyStore,
            _approvals,
            _records,
            Substitute.For<IOutcomeEvidenceStore>(),
            new ApprovalService(Substitute.For<IDispatcher>()),
            Substitute.For<IManifestProvider>(),
            _revisions,
            canonical);

        var req = TestDataBuilder.CreateInvocationRequest(bindingId: "orders-http");
        var res = await handler.Handle(new SubmitInvocationCommand(req, TestDataBuilder.CreatePrincipal(), "idem-1"), TestContext.Current.CancellationToken);

        res.InvocationId.Should().Be("inv-existing");
        res.Decision.Outcome.Should().Be("allow");
    }

    [Fact]
    public async Task SimulateInvocationCommandHandler_ShouldEvaluateWithoutPersistingOrAdmitting()
    {
        var innerNormalizer = Substitute.For<IInvocationNormalizer>();
        innerNormalizer.Modality.Returns(IntentModality.Http);
        var inv = TestDataBuilder.CreateInvocation(id: "sim-1");
        innerNormalizer.NormalizeAsync(Arg.Any<NormalizationInput>(), Arg.Any<CanonicalPrincipal>(), Arg.Any<CancellationToken>())
            .Returns(inv);

        var canonical = new CanonicalJsonService();
        var normalizer = new InvocationNormalizer(new[] { innerNormalizer }, Substitute.For<IManifestProvider>(), canonical, Substitute.For<IOperationRegistry>());
        var evaluator = Substitute.For<IInvocationEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<ToolInvocation>(), Arg.Any<CancellationToken>())
            .Returns(ExecutionDecision.Allow(new[] { "SIM_ALLOW" }, policyRevision: "pol-1"));

        var handler = new SimulateInvocationCommandHandler(normalizer, evaluator, Substitute.For<IManifestProvider>(), _revisions);
        var req = TestDataBuilder.CreateInvocationRequest(bindingId: "orders-http", context: new InvocationContext { WorkflowId = "wf-1", DeclaredPurpose = "test" });

        var res = await handler.Handle(new SimulateInvocationCommand(req, TestDataBuilder.CreatePrincipal()), TestContext.Current.CancellationToken);

        res.InvocationId.Should().Be(inv.Id.ToString());
        res.Decision.Outcome.Should().Be("allow");
        res.Execution.Status.Should().Be("not_started");
        res.Outcome.Status.Should().Be("not_checked");
    }

    [Fact]
    public async Task GetInvocationAndGetInvocationResultQueryHandlers_ShouldAggregateFullLifecycleState()
    {
        var inv = TestDataBuilder.CreateInvocation(id: "inv-1");
        var approval = TestDataBuilder.CreateApproval("app-1", inv);
        var record = TestDataBuilder.CreateExecutionRecord("exec-1", "inv-1");
        record.MarkClaimed();
        var evidence = new OutcomeEvidence("evidence-1", "exec-1", OutcomeStatus.Verified, "ver-1", "1", "verified", "up-1");
        var outcomesStore = Substitute.For<IOutcomeEvidenceStore>();

        _invocations.GetAsync("inv-1", Arg.Any<CancellationToken>()).Returns(inv);
        _invocations.GetDecisionAsync("inv-1", Arg.Any<CancellationToken>())
            .Returns(new RecordedDecision("allow", new[] { "OK" }, "pol-1", DateTimeOffset.UtcNow.AddMinutes(5)));
        _approvals.FindByInvocationAsync("inv-1", Arg.Any<CancellationToken>()).Returns(approval);
        _records.FindByInvocationAsync("inv-1", Arg.Any<CancellationToken>()).Returns(record);
        outcomesStore.GetAsync("exec-1", Arg.Any<CancellationToken>()).Returns(new[] { evidence });

        var getInvHandler = new GetInvocationQueryHandler(_invocations);
        (await getInvHandler.Handle(new GetInvocationQuery("inv-1"), TestContext.Current.CancellationToken)).Should().BeSameAs(inv);

        var getResHandler = new GetInvocationResultQueryHandler(_invocations, _approvals, _records, outcomesStore);
        var dto = await getResHandler.Handle(new GetInvocationResultQuery("inv-1"), TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.InvocationId.Should().Be("inv-1");
        dto.Decision.Outcome.Should().Be("allow");
        dto.Approval!.Id.Should().Be("app-1");
        dto.Approval.Status.Should().Be("pending");
        dto.Execution.Status.Should().Be("claimed");
        dto.Outcome.Status.Should().Be("verified");
    }

    [Fact]
    public void InvocationValidatorsAndLifecycleStatusNames_ShouldValidateAndMapAllEnumValues()
    {
        var principal = TestDataBuilder.CreatePrincipal();
        var validReq = TestDataBuilder.CreateInvocationRequest();

        new SubmitInvocationCommandValidator().Validate(new SubmitInvocationCommand(validReq, principal, "k1")).IsValid.Should().BeTrue();
        new SubmitInvocationCommandValidator().Validate(new SubmitInvocationCommand(
            TestDataBuilder.CreateInvocationRequest(schemaVersion: "v1"), principal, "k1")).IsValid.Should().BeFalse();

        new SimulateInvocationCommandValidator().Validate(new SimulateInvocationCommand(validReq, principal)).IsValid.Should().BeTrue();
        new SimulateInvocationCommandValidator().Validate(new SimulateInvocationCommand(
            TestDataBuilder.CreateInvocationRequest(operationId: ""), principal)).IsValid.Should().BeFalse();

        new ResumeInvocationCommandValidator().Validate(new ResumeInvocationCommand("", principal)).IsValid.Should().BeFalse();
        new ResumeInvocationCommandValidator().Validate(new ResumeInvocationCommand("inv-1", principal)).IsValid.Should().BeTrue();

        new GetInvocationQueryValidator().Validate(new GetInvocationQuery("")).IsValid.Should().BeFalse();
        new GetInvocationQueryValidator().Validate(new GetInvocationQuery("inv-1")).IsValid.Should().BeTrue();

        foreach (var status in Enum.GetValues<ExecutionStatus>())
            LifecycleStatusNames.Execution(status).Should().NotBeNullOrWhiteSpace();

        foreach (var status in Enum.GetValues<OutcomeStatus>())
            LifecycleStatusNames.Outcome(status).Should().NotBeNullOrWhiteSpace();
    }
}
