using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Infrastructure.Features.Identity;

public sealed class LocalPrincipalResolver(IOptions<IdentityOptions> options) : IPrincipalResolver
{
    public string ResolverId => "local-jwt";
    public string ProviderKind => "local";

    private SymmetricSecurityKey Key()
    {
        var bytes = Convert.FromBase64String(options.Value.LocalSigningKeyBase64);
        if (bytes.Length < 32)
            throw new InvalidOperationException("Local signing key must contain at least 256 bits.");
        return new(bytes);
    }

    public Task<CanonicalPrincipal> ResolveAsync(string credential, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var key = Key();
        key.KeyId = "local-signing-key";  // Set a consistent KeyId
        var o = options.Value;

        var principal = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        }
        .ValidateToken(
            credential, 
            new TokenValidationParameters { 
                ValidateIssuer = true,
                ValidIssuer = o.LocalIssuer,
                ValidateAudience = true, 
                ValidAudience = o.Audience, 
                ValidateLifetime = true, 
                RequireExpirationTime = true, 
                RequireSignedTokens = true, 
                ValidateIssuerSigningKey = true, 
                IssuerSigningKey = key, 
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }, 
                ClockSkew = TimeSpan.FromSeconds(30) }, 
        out _);

        return Task.FromResult(PrincipalMapping.Map(principal, o));
    }

    // Trusted provisioning boundary. Never map an unauthenticated request body to these arguments.
    public string Issue(string agentId, string scope, IReadOnlySet<string> authorizedScopes, TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromMinutes(15))
            throw new ArgumentOutOfRangeException(nameof(lifetime));

        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        var o = options.Value;
        var claims = new[]
        {
            new Claim("sub", agentId),
            new Claim(o.SecurityScopeClaim, scope),
            new Claim("scope", string.Join(' ', authorizedScopes)),
            new Claim("jti", Guid.NewGuid().ToString("N"))
        };
        return new JwtSecurityTokenHandler()
            .WriteToken(new JwtSecurityToken(
                o.LocalIssuer, 
                o.Audience, 
                claims, 
                DateTime.UtcNow, 
                DateTime.UtcNow.Add(lifetime), 
                new SigningCredentials(Key(), 
                SecurityAlgorithms.HmacSha256))
            );
    }
}
