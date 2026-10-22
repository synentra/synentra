using Synentra.Infrastructure.Configuration.Identity;

namespace Synentra.Infrastructure.Configuration.Security;

public class IdentityConfiguration
{
    public OidcConfiguration Oidc { get; set; } = new();
    public LdapConfiguration Ldap { get; set; } = new();
}