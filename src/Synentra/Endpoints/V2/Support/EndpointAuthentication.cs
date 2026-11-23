using Synentra.Application.Common.Security;
using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Security;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Endpoints.V2.Support;

internal static class EndpointAuthentication
{
    public static async Task<CanonicalPrincipal> RequirePrincipalAsync(
        this HttpContext context,
        IPrincipalResolver resolver,
        CancellationToken ct)
    {
        var credential = context.Request.GetCredential() ?? throw new AuthenticationApiException(AuthenticationDiagnostics.MissingAgentCredential);

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
            throw new AuthenticationApiException(AuthenticationDiagnostics.CredentialValidationFailed, ex);
        }
    }

    public static async Task<CanonicalPrincipal> RequireAdminAsync(
        this HttpContext context,
        IPrincipalResolver resolver,
        CancellationToken ct)
    {
        var principal = await context.RequirePrincipalAsync(resolver, ct);

        if (principal.Type != PrincipalType.Admin)
            throw new ForbiddenApiException(AuthenticationDiagnostics.AdministratorPermissionRequired);

        return principal;
    }

    public static async Task<CanonicalPrincipal> RequireIssuerAsync(
        this HttpContext context,
        IPrincipalResolver resolver,
        CancellationToken ct)
    {
        var principal = await context.RequirePrincipalAsync(resolver, ct);

        if (principal.Type is not (PrincipalType.Admin or PrincipalType.TrustedIssuer))
            throw new ForbiddenApiException(AuthenticationDiagnostics.TrustedIssuerPermissionRequired);

        return principal;
    }

    private static string? GetCredential(this HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Authorization", out var authorization) ||
            string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        return StripBearer(authorization.ToString());
    }

    private static string StripBearer(string value)
        => value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? value[7..].Trim()
            : value.Trim();
}
