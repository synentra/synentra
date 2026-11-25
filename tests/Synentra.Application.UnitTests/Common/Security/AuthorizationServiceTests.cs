using FluentAssertions;
using NSubstitute;
using Synentra.Application.Common.Security;
using Synentra.Domain.Approvals;
using Synentra.Domain.Grants;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Invocations;
using Synentra.Domain.Invocations.ValueObjects;
using Synentra.Domain.Operations.ValueObjects;
using Synentra.Domain.Grants.ValueObjects;
using Synentra.Domain.Resources.ValueObjects;
using Synentra.Domain.Security;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.UnitTests.Common.Security;

public sealed class AuthorizationServiceTests
{
    private readonly IAdminPolicy _policy = Substitute.For<IAdminPolicy>();
    private readonly AuthorizationService _service;

    public AuthorizationServiceTests() => _service = new AuthorizationService(_policy);

    [Fact]
    public void Constructor_rejects_null_policy()
    {
        var action = () => new AuthorizationService(null!);

        action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("adminPolicy");
    }

    [Theory]
    [InlineData(PrincipalType.Admin, true)]
    [InlineData(PrincipalType.Reviewer, false)]
    [InlineData(PrincipalType.Agent, false)]
    public void CanActivateManifest_allows_only_admins(PrincipalType type, bool expected)
    {
        _service.CanActivateManifest(Principal(type)).Should().Be(expected);
    }

    [Theory]
    [InlineData(PrincipalType.Admin, "other", false)]
    [InlineData(PrincipalType.TrustedIssuer, "issuer", true)]
    [InlineData(PrincipalType.TrustedIssuer, "other", false)]
    [InlineData(PrincipalType.Reviewer, "reviewer", false)]
    public void CanIssueTaskGrant_requires_admin_or_matching_trusted_issuer(PrincipalType type, string issuerId, bool expected)
    {
        _service.CanIssueTaskGrant(Principal(type, agentId: "issuer"), Grant(issuerId)).Should().Be(expected);
    }

    [Fact]
    public void CanApprove_rejects_non_reviewer_and_non_admin() =>
        _service.CanApprove(Principal(PrincipalType.Agent), Approval()).Should().BeFalse();

    [Fact]
    public void CanApprove_rejects_self_approval_when_policy_disallows_it()
    {
        _policy.AllowsSelfApproval.Returns(false);

        _service.CanApprove(Principal(PrincipalType.Reviewer, "requester"), Approval("requester"))
            .Should().BeFalse();
    }

    [Fact]
    public void CanApprove_rejects_cross_scope_when_policy_disallows_it()
    {
        _policy.AllowsSelfApproval.Returns(true);
        _policy.AllowsCrossScopeApproval.Returns(false);

        _service.CanApprove(Principal(PrincipalType.Reviewer, scope: "reviewer-scope"), Approval(scope: "requester-scope"))
            .Should().BeFalse();
    }

    [Fact]
    public void CanApprove_rejects_expired_approval()
    {
        _policy.AllowsCrossScopeApproval.Returns(true);

        _service.CanApprove(Principal(PrincipalType.Admin), Approval(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1)))
            .Should().BeFalse();
    }

    [Fact]
    public void CanApprove_accepts_live_pending_approval() =>
        _service.CanApprove(Principal(PrincipalType.Reviewer), Approval()).Should().BeTrue();

    [Fact]
    public void CanApprove_rejects_resolved_approval()
    {
        var approval = Approval();
        approval.Reject("reviewer");

        _service.CanApprove(Principal(PrincipalType.Reviewer), approval).Should().BeFalse();
    }

    [Theory]
    [InlineData("caller", PrincipalType.Agent, true)]
    [InlineData("other", PrincipalType.Agent, false)]
    [InlineData("other", PrincipalType.Admin, true)]
    public void CanResume_allows_invoking_agent_or_admin(string callerId, PrincipalType type, bool expected) =>
        _service.CanResume(Principal(type, callerId), Invocation("caller")).Should().Be(expected);

    [Theory]
    [InlineData(PrincipalType.Admin, "other", true)]
    [InlineData(PrincipalType.Agent, "issuer", true)]
    [InlineData(PrincipalType.Agent, "other", false)]
    public void CanRevokeGrant_allows_admin_or_issuer(PrincipalType type, string callerId, bool expected) =>
        _service.CanRevokeGrant(Principal(type, callerId), Grant("issuer")).Should().Be(expected);

    [Theory]
    [InlineData(PrincipalType.Admin, true)]
    [InlineData(PrincipalType.Reviewer, true)]
    [InlineData(PrincipalType.Agent, false)]
    public void CanReconcile_allows_admin_and_reviewer(PrincipalType type, bool expected) =>
        _service.CanReconcile(Principal(type)).Should().Be(expected);

    [Theory]
    [InlineData(PrincipalType.Admin, "other", true)]
    [InlineData(PrincipalType.Agent, "scope", true)]
    [InlineData(PrincipalType.Agent, "other", false)]
    public void CanViewAudit_allows_admin_or_matching_scope(PrincipalType type, string scope, bool expected) =>
        _service.CanViewAudit(Principal(type, scope: "scope"), scope).Should().Be(expected);

    private static CanonicalPrincipal Principal(PrincipalType type, string agentId = "agent", string scope = "scope") => new(
        agentId, scope, "issuer", "subject", new HashSet<string>(), new Dictionary<string, string>(), type);

    private static TaskGrant Grant(string issuerId) => new(
        "grant", issuerId, "agent", "scope", Array.Empty<string>(), Array.Empty<ResourceScope>(),
        DateTimeOffset.UtcNow.AddHours(1), null);

    private static Approval Approval(string requestingAgentId = "requester", string scope = "scope", DateTimeOffset? expiresAt = null) => new(
        "approval", InvocationId.New(), new BindingDigest(new string('a', 64)), requestingAgentId,
        "operation", "binding", null, "policy", "review", scope, expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(5));

    private static ToolInvocation Invocation(string authenticatedAgentId) => new(
        InvocationId.New(), 1, authenticatedAgentId, "scope", new OperationId("operation"), "1.0", "family",
        "action", "binding", "1.0", "target", Array.Empty<ResourceDescriptor>(), new Dictionary<string, object>(),
        new Dictionary<string, string>(), new Dictionary<string, string>(), new BindingDigest(new string('b', 64)),
        IntentModality.Http, null, null, null, false, false, DateTimeOffset.UtcNow);
}
