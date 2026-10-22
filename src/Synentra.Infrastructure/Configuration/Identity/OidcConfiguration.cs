namespace Synentra.Infrastructure.Configuration.Identity;

/// <summary>
/// External provider settings (Keycloak, Auth0, Azure AD, Custom)
/// </summary>
public class OidcConfiguration
{
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string SigningKeyBase64 { get; set; } = string.Empty;
    public string? SecurityScopeClaim { get; set; } = string.Empty;
    public bool AgentIdClaim { get; set; } = false;
}