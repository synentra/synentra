using Synentra.Domain.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration.Identity;
using System.Security.Claims;
using System.Text.Json;

namespace Synentra.Infrastructure.Features.Identity;

internal static class PrincipalMapping
{
    public static CanonicalPrincipal Map(
        ClaimsPrincipal principal,
        IdentityConfiguration identityConfiguration)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(identityConfiguration);

        var oidc = identityConfiguration.Providers.Oidc;

        // Map exactly one authenticated identity. Combining identities could
        // accidentally combine one identity's subject with another's roles.
        var identities = principal.Identities
            .Where(identity => identity.IsAuthenticated)
            .ToArray();

        if (identities.Length != 1)
        {
            throw new UnauthorizedAccessException(
                "Exactly one authenticated identity is required.");
        }

        var identity = identities[0];

        var agentIdClaim = RequireClaimName(
            identityConfiguration.Claims.AgentId, nameof(identityConfiguration.Claims.AgentId));

        var securityScopeClaim = RequireClaimName(
            identityConfiguration.Claims.SecurityScope, nameof(identityConfiguration.Claims.SecurityScope));

        var roleClaim = RequireClaimName(
            identityConfiguration.Claims.Roles, nameof(identityConfiguration.Claims.Roles));

        var agentId = RequiredSingle(identity, agentIdClaim);
        var securityScope = RequiredSingle(identity, securityScopeClaim);
        var issuer = RequiredSingle(identity, "iss");
        var subject = RequiredSingle(identity, "sub");

        // OAuth scopes are space-delimited permissions.
        var scopes = identity.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);

        // Roles are separate claim values, not space-delimited scopes.
        // The JWT handler expands the top-level roles array into claims.
        var roles = identity.FindAll(roleClaim)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);

        var type = ResolvePrincipalType(roles);

        return new CanonicalPrincipal(
            agentId,
            securityScope,
            issuer,
            subject,
            scopes,
            BuildClaimDictionary(identity),
            type);
    }

    private static string RequireClaimName(
        string? claimName,
        string settingName)
    {
        if (string.IsNullOrWhiteSpace(claimName))
        {
            throw new InvalidOperationException(
                $"Identity setting '{settingName}' is required.");
        }

        return claimName;
    }

    private static string RequiredSingle(
        ClaimsIdentity identity,
        string claimType)
    {
        // Reject duplicates even when their values are identical.
        var claims = identity.FindAll(claimType)
            .Take(2)
            .ToArray();

        if (claims.Length == 0 ||
            string.IsNullOrWhiteSpace(claims[0].Value))
        {
            throw new UnauthorizedAccessException(
                $"Required identity claim '{claimType}' is missing or empty.");
        }

        if (claims.Length > 1)
        {
            throw new UnauthorizedAccessException(
                $"Required identity claim '{claimType}' occurs more than once.");
        }

        // Preserve exact identifiers: do not trim or normalize them.
        return claims[0].Value;
    }

    private static PrincipalType ResolvePrincipalType(
        HashSet<string> roles)
    {
        // Explicit precedence, matching your existing behavior.
        if (roles.Contains("admin"))
            return PrincipalType.Admin;

        if (roles.Contains("reviewer"))
            return PrincipalType.Reviewer;

        if (roles.Contains("trusted-issuer"))
            return PrincipalType.TrustedIssuer;

        return PrincipalType.Agent;
    }

    private static Dictionary<string, string> BuildClaimDictionary(
        ClaimsIdentity identity)
    {
        return identity.Claims
            .GroupBy(claim => claim.Type, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var values = group
                        .Select(claim => claim.Value)
                        .ToArray();

                    // Compatibility with the existing string-valued contract:
                    // preserve multiple values as a JSON array.
                    return values.Length == 1
                        ? values[0]
                        : JsonSerializer.Serialize(values);
                },
                StringComparer.Ordinal);
    }
}