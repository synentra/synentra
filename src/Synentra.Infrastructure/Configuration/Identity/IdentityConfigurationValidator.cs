using Microsoft.Extensions.Options;
using Synentra.Infrastructure.Configuration.Security;

namespace Synentra.Infrastructure.Configuration.Identity;

public sealed class IdentityConfigurationValidator
    : IValidateOptions<IdentityConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        IdentityConfiguration options)
    {
        var errors = new List<string>();

        var hasOidc = options.Oidc is not null &&
            (!string.IsNullOrWhiteSpace(options.Oidc.Authority) ||
             !string.IsNullOrWhiteSpace(options.Oidc.Audience) ||
             !string.IsNullOrWhiteSpace(options.Oidc.Issuer) ||
             !string.IsNullOrWhiteSpace(options.Oidc.SigningKeyBase64));

        var hasLdap = options.Ldap is not null &&
            (!string.IsNullOrWhiteSpace(options.Ldap.Host) ||
             !string.IsNullOrWhiteSpace(options.Ldap.BaseDn) ||
             !string.IsNullOrWhiteSpace(options.Ldap.UserDnPattern) ||
             !string.IsNullOrWhiteSpace(options.Ldap.BindDn));

        if (!hasOidc && !hasLdap)
        {
            errors.Add(
                "Identity: At least one authentication provider " +
                "(OIDC or LDAP) must be configured.");
        }

        if (hasOidc)
            ValidateOidc(options.Oidc!, errors);

        if (hasLdap)
            ValidateLdap(options.Ldap!, errors);

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateOidc(
        OidcConfiguration options,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.Audience))
            errors.Add("Identity:Oidc:Audience is required.");

        if (string.IsNullOrWhiteSpace(options.Issuer))
            errors.Add("Identity:Oidc:Issuer is required.");

        if (string.IsNullOrWhiteSpace(options.AgentIdClaim))
            errors.Add("Identity:Oidc:AgentIdClaim is required.");

        if (string.IsNullOrWhiteSpace(options.Authority) &&
            string.IsNullOrWhiteSpace(options.SigningKeyBase64))
        {
            errors.Add(
                "Identity:Oidc requires Authority or SigningKeyBase64.");
        }
    }

    private static void ValidateLdap(
        LdapConfiguration options,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
            errors.Add("Identity:Ldap:Host is required.");

        if (string.IsNullOrWhiteSpace(options.BaseDn))
            errors.Add("Identity:Ldap:BaseDn is required.");

        if (string.IsNullOrWhiteSpace(options.UserDnPattern) &&
            string.IsNullOrWhiteSpace(options.BindDn))
        {
            errors.Add(
                "Identity:Ldap requires UserDnPattern or BindDn.");
        }

        if (!string.IsNullOrWhiteSpace(options.BindDn) &&
            string.IsNullOrWhiteSpace(options.BindPassword))
        {
            errors.Add(
                "Identity:Ldap:BindPassword is required when BindDn is set.");
        }
    }
}