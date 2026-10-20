using Synentra.Application.Common.Security;
using Synentra.Domain.Security;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Endpoints.V1;

internal static class ApiEndpointSupport
{
    public static async Task<CanonicalPrincipal> RequirePrincipalAsync(HttpContext context, IPrincipalResolver resolver, CancellationToken ct)
    {
        var credential = GetCredential(context.Request);
        if (credential is null)
            throw new AuthenticationApiException("Missing agent credential.");
        try
        {
            return await resolver.ResolveAsync(credential, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AuthenticationApiException("Credential validation failed.", ex);
        }
    }

    public static async Task<CanonicalPrincipal> RequireAdminAsync(HttpContext context, IPrincipalResolver resolver, CancellationToken ct)
    {
        var principal = await RequirePrincipalAsync(context, resolver, ct);
        if (principal.Type != PrincipalType.Admin)
            throw new ForbiddenApiException("Administrator permission required.");
        return principal;
    }

    public static async Task<CanonicalPrincipal> RequireIssuerAsync(HttpContext context, IPrincipalResolver resolver, CancellationToken ct)
    {
        var principal = await RequirePrincipalAsync(context, resolver, ct);
        if (principal.Type is not (PrincipalType.Admin or PrincipalType.TrustedIssuer))
            throw new ForbiddenApiException("Trusted issuer permission required.");
        return principal;
    }

    public static string? GetIdempotencyKey(HttpRequest request)
        => request.Headers.TryGetValue("Idempotency-Key", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : null;

    public static bool IsSupportedManifestMediaType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return true;
        var media = contentType.Split(';', 2)[0].Trim();
        return media.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || media.Equals("application/yaml", StringComparison.OrdinalIgnoreCase)
            || media.Equals("text/yaml", StringComparison.OrdinalIgnoreCase)
            || media.Equals("application/x-yaml", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetCredential(HttpRequest request)
    {
        if (request.Headers.TryGetValue("Synentra-Authorization", out var synentra) && !string.IsNullOrWhiteSpace(synentra))
            return StripBearer(synentra.ToString());
        if (request.Headers.TryGetValue("Authorization", out var authorization) && !string.IsNullOrWhiteSpace(authorization))
            return StripBearer(authorization.ToString());
        return null;
    }

    private static string StripBearer(string value)
        => value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? value[7..].Trim() : value.Trim();
}

internal sealed class AuthenticationApiException(string message, Exception? inner = null) : Exception(message, inner);
internal sealed class ForbiddenApiException(string message) : Exception(message);
