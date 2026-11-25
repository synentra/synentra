using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Intent.Contracts;
using Synentra.Application.Features.Invocations.Evaluation;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Models;
using Synentra.Application.Features.Obligations.Contracts;
using Synentra.Application.Features.Policies.Contracts;
using Synentra.Application.Features.Resources.Contracts;
using Synentra.Application.Features.Risk.Contracts;
using Synentra.Application.Features.Trust.Contracts;
using Synentra.Application.Tests.TestHelpers;
using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Domain.Governance.Policies;
using Synentra.Domain.Governance.Risk.ValueObjects;
using Synentra.Domain.Governance.Trust.ValueObjects;
using Synentra.Domain.Governance.Trust;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Application.Tests.Features.Invocations;

public sealed class DecisionEngineTests
{
    private readonly IIntentProvider _intentProvider = Substitute.For<IIntentProvider>();
    private readonly IRiskProvider _riskProvider = Substitute.For<IRiskProvider>();
    private readonly ITrustProvider _trustProvider = Substitute.For<ITrustProvider>();
    private readonly IPolicyProvider _policyProvider = Substitute.For<IPolicyProvider>();
    private readonly IResourceResolver _resourceResolver = Substitute.For<IResourceResolver>();
    private readonly IManifestProvider _manifests = Substitute.For<IManifestProvider>();
    private readonly IRevisionProvider _revisions = Substitute.For<IRevisionProvider>();
    private readonly IAuditStore _audit = Substitute.For<IAuditStore>();
    private readonly ILogger<DecisionEngine> _logger = Substitute.For<ILogger<DecisionEngine>>();

    private DecisionEngine CreateSut() => new(
        new[] { _intentProvider },
        _riskProvider,
        _trustProvider,
        _policyProvider,
        new[] { _resourceResolver },
        Array.Empty<IObligationHandler>(),
        _manifests,
        _revisions,
        _audit,
        _logger);

    [Fact]
    public async Task EvaluateAsync_WhenNoActiveBundle_ShouldDenyWithNoActiveBundle()
    {
        _manifests.ActiveBundle.Returns((GovernanceBundleModel?)null);
        var sut = CreateSut();

        var decision = await sut.EvaluateAsync(TestDataBuilder.CreateInvocation(), TestContext.Current.CancellationToken); 

        decision.Outcome.Should().Be(DecisionOutcome.Deny);
        decision.ReasonCodes.Should().Contain("NO_ACTIVE_BUNDLE");
    }

    [Fact]
    public async Task EvaluateAsync_WhenAgentQuarantined_ShouldDenyImmediately()
    {
        _manifests.ActiveBundle.Returns(TestDataBuilder.CreateValidBundle());
        _trustProvider.LoadSnapshotAsync("agent-1", "scope-default", Arg.Any<CancellationToken>())
            .Returns(new TrustSnapshot(new TrustScore(0.0), TrustStatus.Quarantined, "scope-default", true, Array.Empty<TrustFactor>(), DateTimeOffset.UtcNow));

        var sut = CreateSut();
        var decision = await sut.EvaluateAsync(TestDataBuilder.CreateInvocation(), TestContext.Current.CancellationToken);

        decision.Outcome.Should().Be(DecisionOutcome.Deny);
        decision.ReasonCodes.Should().Contain("AGENT_QUARANTINED");
    }

    [Fact]
    public async Task EvaluateAsync_WhenHardRestrictionMatches_ShouldDenyAndAudit()
    {
        var bundle = TestDataBuilder.CreateValidBundle();
        bundle.Spec.Restrictions.Add(new RestrictionModel
        {
            Id = "block-orders",
            Effect = "deny",
            Match = new RestrictionMatchModel { OperationIds = ["orders.create"] }
        });
        _manifests.ActiveBundle.Returns(bundle);
        _trustProvider.LoadSnapshotAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TrustSnapshot(new TrustScore(0.9), TrustStatus.Normal, "scope-default", false, Array.Empty<TrustFactor>(), DateTimeOffset.UtcNow));

        var sut = CreateSut();
        var decision = await sut.EvaluateAsync(TestDataBuilder.CreateInvocation(), TestContext.Current.CancellationToken);

        decision.Outcome.Should().Be(DecisionOutcome.Deny);
        decision.ReasonCodes.Should().Contain("HARD_DENY:block-orders");
        await _audit.Received(1).AppendAsync(Arg.Is<AuditEntry>(a => a.Category == "hard-restriction"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_WhenRequiredIntentFailsAndConfiguredToReview_ShouldReturnReview()
    {
        var bundle = TestDataBuilder.CreateValidBundle();
        bundle.Spec.Profiles[0].Intent!.Required = true;
        bundle.Spec.Profiles[0].Intent!.OnUnavailable = "review";
        _manifests.ActiveBundle.Returns(bundle);
        _revisions.ActiveRevision.Returns("rev-1");

        _trustProvider.LoadSnapshotAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TrustSnapshot(new TrustScore(0.9), TrustStatus.Normal, "scope-default", false, Array.Empty<TrustFactor>(), DateTimeOffset.UtcNow));

        _intentProvider.ProviderId.Returns("intent-default");
        _intentProvider.SupportedModalities.Returns(IntentModality.Http);
        _intentProvider.ClassifyAsync(Arg.Any<IntentEvidence>(), Arg.Any<CancellationToken>())
            .Returns<IntentLabel>(_ => throw new InvalidOperationException("Classifier down"));

        var sut = CreateSut();
        var decision = await sut.EvaluateAsync(TestDataBuilder.CreateInvocation(), TestContext.Current.CancellationToken);

        decision.Outcome.Should().Be(DecisionOutcome.Review);
        decision.ReasonCodes.Should().Contain("INTENT_UNAVAILABLE");
    }

    [Fact]
    public async Task EvaluateAsync_WhenResourceResolutionFails_ShouldDenyWithReason()
    {
        var bundle = TestDataBuilder.CreateValidBundle();
        _manifests.ActiveBundle.Returns(bundle);
        _trustProvider.LoadSnapshotAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TrustSnapshot(new TrustScore(0.9), TrustStatus.Normal, "scope-default", false, Array.Empty<TrustFactor>(), DateTimeOffset.UtcNow));

        _intentProvider.ProviderId.Returns("intent-default");
        _intentProvider.SupportedModalities.Returns(IntentModality.Http);
        _intentProvider.ClassifyAsync(Arg.Any<IntentEvidence>(), Arg.Any<CancellationToken>())
            .Returns(new IntentLabel("create_order", 0.95, IntentStatus.Available, "intent-default"));

        _riskProvider.EvaluateAsync(Arg.Any<RiskInput>(), Arg.Any<CancellationToken>())
            .Returns(new RiskAssessment(new RiskScore(0.1, 1.0), Array.Empty<string>(), "risk-v1", Array.Empty<string>(), Array.Empty<RiskFactorResult>()));

        _resourceResolver.ResolverId.Returns("order-resolver");
        _resourceResolver.Version.Returns("1");
        _resourceResolver.ResolveAsync(Arg.Any<Domain.Invocations.ToolInvocation>(), Arg.Any<ResolverRegistration>(), Arg.Any<CancellationToken>())
            .Returns(ResourceResolutionResult.NotFound("Order not found"));

        var sut = CreateSut();
        var decision = await sut.EvaluateAsync(TestDataBuilder.CreateInvocation(), TestContext.Current.CancellationToken);

        decision.Outcome.Should().Be(DecisionOutcome.Deny);
        decision.ReasonCodes.Should().Contain("RESOURCE_NOT_FOUND");
    }

    [Fact]
    public async Task EvaluateAsync_WhenAllChecksPassAndPolicyAllows_ShouldAllowWithFullEvidenceAndAudit()
    {
        var bundle = TestDataBuilder.CreateValidBundle();
        _manifests.ActiveBundle.Returns(bundle);
        _revisions.ActiveRevision.Returns("rev-1");

        _trustProvider.LoadSnapshotAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TrustSnapshot(new TrustScore(0.95), TrustStatus.Normal, "scope-default", false, Array.Empty<TrustFactor>(), DateTimeOffset.UtcNow));

        _intentProvider.ProviderId.Returns("intent-default");
        _intentProvider.SupportedModalities.Returns(IntentModality.Http);
        _intentProvider.ClassifyAsync(Arg.Any<IntentEvidence>(), Arg.Any<CancellationToken>())
            .Returns(new IntentLabel("create_order", 0.98, IntentStatus.Available, "intent-default"));

        _riskProvider.EvaluateAsync(Arg.Any<RiskInput>(), Arg.Any<CancellationToken>())
            .Returns(new RiskAssessment(new RiskScore(0.15, 1.0), Array.Empty<string>(), "risk-v1", Array.Empty<string>(), Array.Empty<RiskFactorResult>()));

        var resolvedResource = new ResourceDescriptor("order", "order", "ord-123", "1");
        _resourceResolver.ResolverId.Returns("order-resolver");
        _resourceResolver.Version.Returns("1");
        _resourceResolver.ResolveAsync(Arg.Any<Domain.Invocations.ToolInvocation>(), Arg.Any<ResolverRegistration>(), Arg.Any<CancellationToken>())
            .Returns(ResourceResolutionResult.Available(new[] { resolvedResource }));

        _policyProvider.EvaluateAsync(Arg.Any<PolicyInput>(), Arg.Any<CancellationToken>())
            .Returns(PolicyResult.Allow(new[] { "RULE_MATCHED" }, "policy-rev-1"));

        var sut = CreateSut();
        var decision = await sut.EvaluateAsync(TestDataBuilder.CreateInvocation(), TestContext.Current.CancellationToken);

        decision.Outcome.Should().Be(DecisionOutcome.Allow);
        decision.PolicyRevision.Should().Be("policy-rev-1");
        decision.Evidence.ManifestRevision.Should().Be("rev-1");
        decision.Evidence.Resources.Should().ContainSingle().Which.Should().Be(resolvedResource);

        await _audit.Received(1).AppendAsync(Arg.Is<AuditEntry>(a => a.Category == "decision" && a.Details["outcome"] == "Allow"), Arg.Any<CancellationToken>());
    }
}
