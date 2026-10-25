using Microsoft.Extensions.Options;
using Synentra.Infrastructure.Configuration.Security;

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

        if (string.IsNullOrWhiteSpace(options.DefaultProvider))
        {
            errors.Add("Identity:DefaultProvider is required.");
        }
        else if (options.Providers is null)
        {
            errors.Add("Identity:Providers is required.");
        }
        else
        {
            switch (options.DefaultProvider.ToLowerInvariant())
            {
                case OidcProvider:
                    ValidateOidc(options.Providers.Oidc, errors);
                    break;

                case LdapProvider:
                    ValidateLdap(options.Providers.Ldap, errors);
                    break;

                default:
                    errors.Add(
                        "Identity:DefaultProvider must be one of: " +
                        "local, oidc, ldap.");
                    break;
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
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
            ValidateHttpsUrl(
                options.Authority,
                $"{prefix}:Authority",
                errors);
        }

        if (!string.IsNullOrWhiteSpace(options.SigningKeyBase64))
        {
            ValidateSigningKey(
                options.SigningKeyBase64,
                $"{prefix}:SigningKeyBase64",
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

        Require(
            options.AgentIdClaim,
            $"{prefix}:AgentIdClaim",
            errors);

        Require(
            options.SecurityScopeClaim,
            $"{prefix}:SecurityScopeClaim",
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
            options.Host,
            $"{prefix}:Host",
            errors);

        if (!string.IsNullOrWhiteSpace(options.Host))
        {
            if (Uri.CheckHostName(options.Host) ==
                UriHostNameType.Unknown)
            {
                errors.Add(
                    $"{prefix}:Host must be a valid " +
                    "hostname or IP address.");
            }
        }

        if (options.Port is < 1 or > 65535)
        {
            errors.Add(
                $"{prefix}:Port must be between 1 and 65535.");
        }

        Require(
            options.BaseDn,
            $"{prefix}:BaseDn",
            errors);

        var hasServiceDn = !string.IsNullOrWhiteSpace(
            options.ServiceDn);

        var hasServicePassword = !string.IsNullOrWhiteSpace(
            options.ServicePassword);

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

    private static void ValidateSigningKey(
        string? keyBase64,
        string property,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            errors.Add($"{property} is required.");
            return;
        }

        try
        {
            var bytes = Convert.FromBase64String(keyBase64);

            if (bytes.Length < MinSigningKeyLengthBytes)
            {
                errors.Add(
                    $"{property} must decode to at least " +
                    $"{MinSigningKeyLengthBytes} bytes (256 bits).");
            }
        }
        catch (FormatException)
        {
            errors.Add(
                $"{property} must contain a valid Base64-encoded key.");
        }
    }

    private static void ValidateHttpsUrl(
        string? value,
        string property,
        List<string> errors)
    {
        if (!Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            errors.Add(
                $"{property} must be a valid HTTPS URL " +
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