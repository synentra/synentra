namespace Synentra.Infrastructure.Features.Identity;

public sealed class IdentityOptions
{
    public string Provider { get; set; } = "local";
    public string Authority { get; set; } = "";
    public string Audience { get; set; } = "synentra-agents";
    public string LocalIssuer { get; set; } = "synentra";
    public string LocalSigningKeyBase64 { get; set; } = "";
    public string SecurityScopeClaim { get; set; } = "security_scope";
    public string AgentIdClaim { get; set; } = "sub";
    public string LdapHost { get; set; } = "";
    public int LdapPort { get; set; } = 636;
    public string LdapBaseDn { get; set; } = "";
    public string LdapServiceDn { get; set; } = "";
    public string LdapServicePassword { get; set; } = "";
    public string LdapAdminGroupDn { get; set; } = "";
    public string LdapReviewerGroupDn { get; set; } = "";
}
