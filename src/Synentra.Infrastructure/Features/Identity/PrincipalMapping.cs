using System.Security.Claims;
using Synentra.Domain.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration.Security;

namespace Synentra.Infrastructure.Features.Identity;

internal static class PrincipalMapping
{
    public static CanonicalPrincipal Map(ClaimsPrincipal principal, IdentityConfiguration identityConfiguration)
    {
        string Required(string name) => principal.FindFirst(name)?.Value is { Length: > 0 } value 
            ? value 
            : throw new UnauthorizedAccessException("Required identity claim missing.");

        var scopes = principal
                    .FindAll("scope")
                    .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .ToHashSet(StringComparer.Ordinal);

        var type = scopes.Contains("synentra.admin") 
            ? PrincipalType.Admin 
            : scopes.Contains("synentra.reviewer") ? PrincipalType.Reviewer : scopes.Contains("synentra.trusted-issuer") ? PrincipalType.TrustedIssuer : PrincipalType.Agent;

        return new(
            Required(identityConfiguration.Providers.Oidc.AgentIdClaim), 
            Required(identityConfiguration.Providers.Oidc.SecurityScopeClaim), 
            Required("iss"), 
            Required("sub"), 
            scopes, 
            principal.Claims.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.First().Value), 
            type);
    }
}
