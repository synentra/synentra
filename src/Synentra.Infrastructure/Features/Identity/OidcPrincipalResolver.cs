using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration.Security;
using System.Security.Claims;

namespace Synentra.Infrastructure.Features.Identity;

public sealed class OidcPrincipalResolver : IPrincipalResolver, IDisposable
{
    private const int MaximumTokenSizeInBytes = 32 * 1024;

    private readonly IdentityConfiguration options;
    private readonly string expectedIssuer;
    private readonly HttpClient http;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> configuration;
    private readonly JsonWebTokenHandler tokenHandler;
    private readonly TokenValidationParameters validationParameters;

    public string ResolverId => "oidc";
    public string ProviderKind => "oidc";

    public OidcPrincipalResolver(
        IOptions<IdentityConfiguration> options,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        this.options = options.Value;
        var oidc = this.options.Providers.Oidc;

        // Validate everything before allocating the owned HttpClient.
        var authority = ParseEndpoint(oidc.Authority, "OIDC authority");

        if (!oidc.RequireHttpsMetadata && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Disabling HTTPS metadata is allowed only in Development.");
        }

        if (oidc.RequireHttpsMetadata &&
            authority.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "OIDC authority must use HTTPS.");
        }

        if (string.IsNullOrWhiteSpace(oidc.Audience))
        {
            throw new InvalidOperationException(
                "OIDC audience is required.");
        }

        // Authority locates discovery. Issuer is the exact identifier
        // expected in discovery and in the token's 'iss' claim.
        var authorityUrl = oidc.Authority.TrimEnd('/');

        expectedIssuer = string.IsNullOrWhiteSpace(oidc.Issuer)
            ? authorityUrl
            : oidc.Issuer;

        var issuerUri = ParseEndpoint(expectedIssuer, "OIDC issuer");

        if (oidc.RequireHttpsMetadata &&
            issuerUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "OIDC issuer must use HTTPS.");
        }

        // Disable redirects so the configured metadata endpoint must be
        // directly reachable. TLS certificate validation remains enabled.
        http = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        })
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var documentRetriever = new HttpDocumentRetriever(http)
        {
            RequireHttps = oidc.RequireHttpsMetadata
        };

        configuration =
            new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{authorityUrl}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                documentRetriever);

        // Keep the manager's standard refresh throttling and
        // last-known-good configuration behavior.
        tokenHandler = new JsonWebTokenHandler
        {
            MapInboundClaims = false,
            MaximumTokenSizeInBytes = MaximumTokenSizeInBytes
        };

        validationParameters = new TokenValidationParameters
        {
            // Let IdentityModel obtain and refresh verification keys.
            ConfigurationManager = configuration,

            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,

            ValidateIssuer = true,
            ValidIssuer = expectedIssuer,

            // Pin the issuer explicitly. This prevents metadata from
            // introducing an additional accepted issuer.
            IssuerValidator = (issuer, _, _) =>
            {
                if (!string.Equals(
                        issuer,
                        expectedIssuer,
                        StringComparison.Ordinal))
                {
                    throw new SecurityTokenInvalidIssuerException(
                        "Token issuer does not match the configured issuer.");
                }

                return issuer;
            },

            ValidateAudience = true,
            RequireAudience = true,
            ValidAudience = oidc.Audience,

            // Treat audience identifiers as exact strings.
            IgnoreTrailingSlashWhenValidatingAudience = false,

            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            // Explicitly supported asymmetric signing algorithms.
            ValidAlgorithms = new[]
            {
                SecurityAlgorithms.RsaSha256,
                SecurityAlgorithms.EcdsaSha256
            },

            AuthenticationType = "oidc",
            NameClaimType = "preferred_username",
            RoleClaimType = oidc.RoleClaim,

            // Do not attach the credential to failed validation results.
            IncludeTokenOnFailedValidation = false,
            SaveSigninToken = false
        };
    }

    public async Task WarmupAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var document = await configuration
            .GetConfigurationAsync(ct)
            .WaitAsync(ct)
            .ConfigureAwait(false);

        ValidateMetadata(document);
    }

    public async Task<CanonicalPrincipal> ResolveAsync(
        string credential,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // The caller must extract the raw JWT from the HTTP header.
        // Do not pass "Bearer <token>" into this method.
        if (string.IsNullOrWhiteSpace(credential))
        {
            throw new SecurityTokenException(
                "An access token is required.");
        }

        // Compact JWTs are ASCII, so this also bounds their byte size.
        if (credential.Length > MaximumTokenSizeInBytes)
        {
            throw new SecurityTokenException(
                "The access token exceeds the permitted size.");
        }

        // This resolver accepts signed compact JWTs (JWS), not encrypted
        // JWTs (JWE). Encrypted tokens require a separate decryption design.
        if (credential.Count(character => character == '.') != 2 ||
            !tokenHandler.CanReadToken(credential))
        {
            throw new SecurityTokenException(
                "The access token is not a supported signed JWT.");
        }

        // Usually served from cache. This also ensures metadata validation
        // happens even if startup did not invoke WarmupAsync.
        var document = await configuration
            .GetConfigurationAsync(ct)
            .WaitAsync(ct)
            .ConfigureAwait(false);

        ValidateMetadata(document);

        // Clone so per-validation internal changes are not shared
        // between concurrent requests.
        var parameters = validationParameters.Clone();

        var result = await tokenHandler
            .ValidateTokenAsync(credential, parameters)
            .WaitAsync(ct)
            .ConfigureAwait(false);

        ct.ThrowIfCancellationRequested();

        // Never map claims from an unsuccessful validation result.
        if (!result.IsValid)
        {
            throw new SecurityTokenException(
                "Access token validation failed.",
                result.Exception);
        }

        if (result.ClaimsIdentity is not { IsAuthenticated: true } identity)
        {
            throw new SecurityTokenException(
                "The access token did not produce an authenticated identity.");
        }

        var principal = new ClaimsPrincipal(identity);

        // Preserve your application's canonical identity mapping.
        // This mapper must reject missing/ambiguous required agent-ID
        // and security-scope claims.
        return PrincipalMapping.Map(principal, options);
    }

    private void ValidateMetadata(OpenIdConnectConfiguration document)
    {
        if (!string.Equals(
                document.Issuer,
                expectedIssuer,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "OIDC metadata issuer does not match the configured issuer.");
        }

        if (document.SigningKeys.Count == 0)
        {
            throw new InvalidOperationException(
                "OIDC metadata does not contain verification keys.");
        }
    }

    private static Uri ParseEndpoint(string value, string settingName)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException(
                $"{settingName} must be an absolute HTTP(S) URL without credentials, query parameters, or fragments.");
        }

        return uri;
    }

    public void Dispose()
    {
        http.Dispose();
    }
}