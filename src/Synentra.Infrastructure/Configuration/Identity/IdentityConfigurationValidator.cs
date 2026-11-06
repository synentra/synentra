using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Identity;

public sealed class IdentityConfigurationValidator
    : IValidateOptions<IdentityConfiguration>
{
    private const string OidcProvider = "oidc";
    private const string LdapProvider = "ldap";

    private const int MinSigningKeyLengthBytes = 32;

    public ValidateOptionsResult Validate(
        string? name,
        IdentityConfiguration options)
    {
        var errors = new List<string>();

        ValidateClaims(options.Claims, errors);

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            errors.Add("Identity:Provider is required.");
        }
        else if (options.Providers is null)
        {
            errors.Add("Identity:Providers is required.");
        }
        else
        {
            switch (options.Provider.ToLowerInvariant())
            {
                case OidcProvider:
                    ValidateOidc(options.Providers.Oidc, errors);
                    break;

                case LdapProvider:
                    ValidateLdap(options.Providers.Ldap, errors);
                    break;

                default:
                    errors.Add(
                        "Identity:Provider must be one of: " +
                        "local, oidc, ldap.");
                    break;
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateClaims(
    IdentityClaimConfiguration options,
    List<string> errors)
    {
        const string prefix = "Identity:Claims";

        Require(
            options.AgentId,
            $"{prefix}:AgentId",
            errors);

        Require(
            options.Roles,
            $"{prefix}:Roles",
            errors);

        Require(
            options.SecurityScope,
            $"{prefix}:SecurityScopeClaim",
            errors);
    }

    private static void ValidateOidc(
        OidcConfiguration? options,
        List<string> errors)
    {
        const string prefix = "Identity:Providers:Oidc";

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        ValidateCommonJwt(options, errors);

        Require(
            options.Authority,
            $"{prefix}:Authority",
            errors);

        if (!string.IsNullOrWhiteSpace(options.Authority))
        {
            ValidateUrl(
                options.Authority,
                $"{prefix}:Authority",
                errors);
        }
    }

    private static void ValidateCommonJwt(
        OidcConfiguration options,
        List<string> errors)
    {
        const string prefix = "Identity:Providers:Oidc";

        Require(
            options.Issuer,
            $"{prefix}:Issuer",
            errors);

        Require(
            options.Audience,
            $"{prefix}:Audience",
            errors);
    }

    private static void ValidateLdap(
        LdapConfiguration? options,
        List<string> errors)
    {
        const string prefix = "Identity:Providers:Ldap";

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        Require(
            options.Endpoint,
            $"{prefix}:Endpoint",
            errors);

        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            if (Uri.CheckHostName(options.Endpoint) ==
                UriHostNameType.Unknown)
            {
                errors.Add(
                    $"{prefix}:Endpoint must be a valid " +
                    "hostname or IP address.");
            }
        }

        Require(
            options.BaseDn,
            $"{prefix}:BaseDn",
            errors);

        var hasServiceDn = !string.IsNullOrWhiteSpace(
            options.BindDn);

        var hasServicePassword = !string.IsNullOrWhiteSpace(
            options.BindPasswordSecretRef);

        if (hasServiceDn && !hasServicePassword)
        {
            errors.Add(
                $"{prefix}:ServicePassword is required " +
                "when ServiceDn is configured.");
        }

        if (!hasServiceDn && hasServicePassword)
        {
            errors.Add(
                $"{prefix}:ServiceDn is required " +
                "when ServicePassword is configured.");
        }

        // Group DNs are optional.
        // Validate their presence only if the corresponding
        // authorization roles are mandatory for the deployment.
    }

    private static void ValidateUrl(
        string? value,
        string property,
        List<string> errors)
    {
        if (!Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            errors.Add(
                $"{property} must be a valid URL " +
                "without credentials, query, or fragment.");
        }
    }

    private static void Require(
        string? value,
        string property,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{property} is required.");
        }
    }
}