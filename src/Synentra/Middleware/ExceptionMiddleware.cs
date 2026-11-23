using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Synentra.Application.Features.Invocations.Exceptions;
using Synentra.Application.Features.Manifests.Exceptions;
using Synentra.Diagnostics;
using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;

namespace Synentra.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        try { await next(context); }

        catch (Exception ex)
        {
            context.Items["ErrorType"] = ex.GetType().Name;
            var (status, diagnostic) = Map(ex);
            if (status >= 500)
                logger.LogError("Request failed ({ErrorType}).", ex.GetType().Name);
            else
                logger.LogWarning("API request rejected: {Code}", diagnostic.Code);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails { Title = diagnostic.Message, Status = status, Type = $"https://httpstatuses.com/{status}" };
            problem.Extensions["errorCode"] = diagnostic.Code;
            problem.Extensions["traceId"] = context.TraceIdentifier;
            problem.Extensions["correlationId"] = correlationId;
            if (ex is ValidationException validation)
                problem.Extensions["errors"] = validation.Errors.GroupBy(x => x.PropertyName).ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var value)
            && Guid.TryParse(value.ToString(), out var supplied))
            return supplied.ToString("D");

        return Guid.NewGuid().ToString("D");
    }

    private static (int Status, Diagnostic diagnostic) Map(Exception ex)
    {
        if ((ex is CredentialNotFoundException))
            return (503, SecretDiagnostics.CredentialUnavailable);

        if (ex is Microsoft.EntityFrameworkCore.DbUpdateException) 
            return (409, PersistenceDiagnostics.CredentialWriteConflict);

        if (ex is ArgumentException)
            return (400, InfrastructureDiagnostics.InvalidRequest);

        if (ex is ValidationException)
            return (400, InfrastructureDiagnostics.ValidationFailed);

        if (ex is ActivationConflictException)
            return (409, ManifestDiagnostics.ActivationConflict(ex.Message));

        if (ex is AuthorityExpiredException) 
            return (410, InvocationDiagnostics.AuthorityExpired(ex.Message));

        if (ex is BundleParseException bundle && bundle.Issues.Contains("SIZE_EXCEEDED")) 
            return (413, ManifestDiagnostics.PayloadTooLarge);

        if (ex is BundleParseException bundleParse) 
            return (400, ManifestDiagnostics.InvalidManifest(bundleParse.Message));

        if (ex is AuthenticationApiException authenticationApiException) 
            return (401, authenticationApiException.Diagnostic);

        if (ex is ForbiddenApiException forbiddenApiException) 
            return (403, forbiddenApiException.Diagnostic);

        if (ex is UnauthorizedAccessException) 
            return (403, AuthenticationDiagnostics.UnauthorizedAccess);

        if (ex is KeyNotFoundException) 
            return (404, InfrastructureDiagnostics.KeyNotFound(ex.Message));

        if (ex is SynentraException be) 
            return (be.Diagnostic.Category switch
            {
                DiagnosticCategory.Validation => 400,
                DiagnosticCategory.Authentication => 401,
                DiagnosticCategory.Forbidden => 403,
                DiagnosticCategory.NotFound => 404,
                DiagnosticCategory.Conflict => 409,
                _ => 500
            }, be.Diagnostic);

        if (ex is InvalidOperationException ioe)
        {
            if (ioe.Message.Contains("CONFLICT", StringComparison.OrdinalIgnoreCase)) 
                return (409, InfrastructureDiagnostics.ConflictsRequest);

            if (ioe.Message.Contains("REQUIRED", StringComparison.OrdinalIgnoreCase) 
                || ioe.Message.Contains("AMBIGUOUS", StringComparison.OrdinalIgnoreCase) 
                || ioe.Message.Contains("INVALID", StringComparison.OrdinalIgnoreCase)) 
                return (400, InfrastructureDiagnostics.InvalidRequest);

            if (ioe.Message.Contains("NO_ACTIVE_BUNDLE", StringComparison.Ordinal)) 
                return (503, ManifestDiagnostics.NoActiveBundle);

            if (ioe.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase)) 
                return (503, GovernanceDiagnostics.GovernanceDependencyUnavailable);
        }

        return (500, InfrastructureDiagnostics.InternalError);
    }
}