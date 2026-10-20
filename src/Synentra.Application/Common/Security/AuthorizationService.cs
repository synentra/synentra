using Synentra.Domain.Approvals;
using Synentra.Domain.Grants;
using Synentra.Domain.Invocations;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Domain.Security;

namespace Synentra.Application.Common.Security;

public sealed class AuthorizationService
{
    private readonly IAdminPolicy _adminPolicy;

    public AuthorizationService(IAdminPolicy adminPolicy)
    {
        _adminPolicy = adminPolicy ?? throw new ArgumentNullException(nameof(adminPolicy));
    }

    public bool CanActivateManifest(CanonicalPrincipal principal) => principal.Type == PrincipalType.Admin;

    public bool CanIssueTaskGrant(CanonicalPrincipal principal, TaskGrant grant) 
        => principal.Type is PrincipalType.Admin or PrincipalType.TrustedIssuer && grant.IssuerId == principal.AgentId;

    public bool CanApprove(CanonicalPrincipal reviewer, Approval approval)
    {
        if (reviewer.Type != PrincipalType.Reviewer && reviewer.Type != PrincipalType.Admin)
            return false;

        // Self-approval denied unless explicit admin policy permits it
        if (reviewer.AgentId == approval.RequestingAgentId && !_adminPolicy.AllowsSelfApproval)
            return false;

        // Scope boundary: reviewer must be in the same security scope as the
        // requesting agent unless explicitly permitted to cross scopes.
        if (!_adminPolicy.AllowsCrossScopeApproval && reviewer.SecurityScope != approval.SecurityScope)
            return false;

        // Approval must still be live and pending
        var now = DateTimeOffset.UtcNow;
        if (now >= approval.ExpiresAt)
            return false;

        return approval.Status == ApprovalStatus.Pending;
    }

    public bool CanResume(CanonicalPrincipal caller, ToolInvocation invocation)
    {
        // Only the invoking agent (or an admin) may resume — never a cross-agent caller
        return caller.AgentId == invocation.AuthenticatedAgentId || caller.Type == PrincipalType.Admin;
    }

    public bool CanRevokeGrant(CanonicalPrincipal by, TaskGrant grant) 
        => by.Type == PrincipalType.Admin || by.AgentId == grant.IssuerId;

    public bool CanReconcile(CanonicalPrincipal by) 
        => by.Type is PrincipalType.Admin or PrincipalType.Reviewer;

    public bool CanViewAudit(CanonicalPrincipal by, string securityScope) 
        => by.Type == PrincipalType.Admin || by.SecurityScope == securityScope;
}
