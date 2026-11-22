using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Synentra.Application.Features.Invocations.Exceptions;
using Synentra.Application.Features.Manifests.Exceptions;
using Synentra.Domain.Common.Diagnostics;
using Synentra.Domain.Common.Exceptions;
using Synentra.Endpoints.V2;
using Synentra.Exceptions;

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
            var (status, code, title) = Map(ex);
            if (status >= 500) logger.LogError("Request failed ({ErrorType}).", ex.GetType().Name); else logger.LogWarning("API request rejected: {Code}", code);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails { Title = title, Status = status, Type = $"https://httpstatuses.com/{status}" };
            problem.Extensions["errorCode"] = code;
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

    private static (int Status, string Code, string Title) Map(Exception ex)
    {
        if (ex is Synentra.Application.Common.Security.CredentialNotFoundException)
            return (503, "CREDENTIAL_UNAVAILABLE", "A required credential is unavailable.");

        if (ex is Microsoft.EntityFrameworkCore.DbUpdateException) 
            return (409, "CREDENTIAL_WRITE_CONFLICT", "Concurrent or duplicate write rejected.");

        if (ex is ArgumentException) 
            return (400, "INVALID_REQUEST", "Request is invalid.");

        if (ex is ValidationException) 
            return (400, "VALIDATION_FAILED", "Request validation failed.");

        if (ex is ActivationConflictException) 
            return (409, "ACTIVATION_CONFLICT", ex.Message);

        if (ex is AuthorityExpiredException) 
            return (410, "AUTHORITY_EXPIRED", ex.Message);

        if (ex is BundleParseException bundle && bundle.Issues.Contains("SIZE_EXCEEDED")) 
            return (413, "PAYLOAD_TOO_LARGE", "Manifest exceeds the configured size limit.");

        if (ex is BundleParseException bundleParse) 
            return (400, bundleParse.Issues.FirstOrDefault() ?? "INVALID_MANIFEST", bundleParse.Message);

        if (ex is AuthenticationApiException authenticationApiException) 
            return (401, authenticationApiException.Diagnostic.Code.ToString(), authenticationApiException.Diagnostic.Message ?? "Request failed.");

        if (ex is ForbiddenApiException forbiddenApiException) 
            return (403, forbiddenApiException.Diagnostic.Code.ToString(), forbiddenApiException.Diagnostic.Message ?? "Request failed.");

        if (ex is UnauthorizedAccessException) 
            return (403, "FORBIDDEN", "The authenticated principal is not authorized for this operation.");

        if (ex is KeyNotFoundException) 
            return (404, "NOT_FOUND", ex.Message);

        if (ex is SynentraException be) 
            return (be.Diagnostic.Category switch
            {
                DiagnosticCategory.Validation => 400,
                DiagnosticCategory.Authentication => 401,
                DiagnosticCategory.Forbidden => 403,
                DiagnosticCategory.NotFound => 404,
                DiagnosticCategory.Conflict => 409,
                _ => 500
            }, be.Diagnostic.Code.ToString(), be.Diagnostic.Message ?? "Request failed.");

        if (ex is InvalidOperationException ioe)
        {
            if (ioe.Message.Contains("CONFLICT", StringComparison.OrdinalIgnoreCase)) 
                return (409, ioe.Message, "Request conflicts with existing state.");

            if (ioe.Message.Contains("REQUIRED", StringComparison.OrdinalIgnoreCase) 
                || ioe.Message.Contains("AMBIGUOUS", StringComparison.OrdinalIgnoreCase) 
                || ioe.Message.Contains("INVALID", StringComparison.OrdinalIgnoreCase)) 
                return (400, ioe.Message, "Request is invalid.");

            if (ioe.Message.Contains("NO_ACTIVE_BUNDLE", StringComparison.Ordinal)) 
                return (503, "NO_ACTIVE_BUNDLE", "No active governance bundle is available.");

            if (ioe.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase)) 
                return (503, "GOVERNANCE_DEPENDENCY_UNAVAILABLE", "A required governance dependency is unavailable.");
        }

        return (500, "INTERNAL_ERROR", "An unexpected error occurred.");
    }
}