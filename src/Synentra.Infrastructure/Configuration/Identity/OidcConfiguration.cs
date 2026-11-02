namespace Synentra.Infrastructure.Configuration.Identity;

/// <summary>
/// External provider settings (Keycloak, Auth0, Azure AD, Custom)
/// </summary>
public class OidcConfiguration
{
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
    public string SecurityScopeClaim { get; set; } = "security_scope";
    public string AgentIdClaim { get; set; } = "sub";
    public string RoleClaim { get; set; } = "roles";
}