using Synentra.Infrastructure.Configuration.Identity;

namespace Synentra.Infrastructure.Configuration.Security;

public class IdentityConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Identity";

    public OidcConfiguration Oidc { get; set; } = new();
    public LdapConfiguration Ldap { get; set; } = new();
}