using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Extensions.Options;
using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Infrastructure.Features.Identity;

public sealed class OidcPrincipalResolver : IPrincipalResolver
{
    private readonly IdentityOptions options;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> configuration;
    public OidcPrincipalResolver(IOptions<IdentityOptions> options)
    {
        this.options = options.Value;
        if (!Uri.TryCreate(this.options.Authority, UriKind.Absolute, out var authority) || authority.Scheme != "https")
            throw new InvalidOperationException("OIDC authority must use HTTPS.");

        configuration = new ConfigurationManager<OpenIdConnectConfiguration>(this.options.Authority.TrimEnd('/') + "/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever { RequireHttps = true });
    }

    public string ResolverId => "oidc";
    public string ProviderKind => "oidc";

    public async Task<CanonicalPrincipal> ResolveAsync(string credential, CancellationToken ct)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var config = await configuration.GetConfigurationAsync(ct);
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Authority.TrimEnd('/'),
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidAlgorithms = new[]
                {
                    SecurityAlgorithms.RsaSha256,
                    SecurityAlgorithms.EcdsaSha256
                }
            };
            try
            {
                var principal = new JwtSecurityTokenHandler
                {
                    MapInboundClaims = false
                }.ValidateToken(credential, parameters, out _);
                return PrincipalMapping.Map(principal, options);
            }
            catch (SecurityTokenSignatureKeyNotFoundException)when (attempt == 0)
            {
                configuration.RequestRefresh();
            }
        }

        throw new UnauthorizedAccessException("Token signing key unavailable.");
    }
}
