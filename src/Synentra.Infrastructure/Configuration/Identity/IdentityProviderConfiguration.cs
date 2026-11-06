namespace Synentra.Infrastructure.Configuration.Identity;

public class IdentityProviderConfiguration
{
    public OidcConfiguration Oidc { get; set; } = new();
    public LdapConfiguration Ldap { get; set; } = new();
}