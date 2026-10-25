namespace Synentra.Infrastructure.Configuration.Identity;

/// <summary>
/// External provider settings (Keycloak, Auth0, Azure AD, Custom)
/// </summary>
public class OidcConfiguration
{
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string LocalSigningKeyId { get; set; } = "current";
    public string LocalSigningKeyBase64 { get; set; } = "";
    public string SigningKeyBase64 { get; set; } = string.Empty;
    public Dictionary<string, string> LocalValidationKeys { get; set; } = new(StringComparer.Ordinal);
    public string SecurityScopeClaim { get; set; } = "security_scope";
    public string AgentIdClaim { get; set; } = "sub";
}