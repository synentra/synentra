using Microsoft.AspNetCore.Mvc;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Endpoints.V2.Support;

internal static class ApiProblemDetailsExtensions
{
    public static IResult Problem(
        this HttpContext context,
        int status,
        string code,
        string title)
        => Create(context, status, code, title);

    public static IResult BadRequest(
        this HttpContext context,
        string code = "BAD_REQUEST",
        string title = "The request is invalid.")
        => Create(context, StatusCodes.Status400BadRequest, code, title);

    public static IResult Unauthorized(
        this HttpContext context,
        string code = "UNAUTHORIZED",
        string title = "Authentication is required.")
        => Create(context, StatusCodes.Status401Unauthorized, code, title);

    public static IResult Forbidden(
        this HttpContext context,
        string code = "FORBIDDEN",
        string title = "You are not authorized to perform this operation.")
        => Create(context, StatusCodes.Status403Forbidden, code, title);

    public static IResult NotFound(
        this HttpContext context,
        string code = "NOT_FOUND",
        string title = "The requested resource was not found.")
        => Create(context, StatusCodes.Status404NotFound, code, title);

    public static IResult Conflict(
        this HttpContext context,
        string code = "CONFLICT",
        string title = "The request conflicts with the current resource state.")
        => Create(context, StatusCodes.Status409Conflict, code, title);

    public static IResult UnsupportedMediaType(
        this HttpContext context,
        string code = "UNSUPPORTED_MEDIA_TYPE",
        string title = "The request media type is not supported.")
        => Create(
            context,
            StatusCodes.Status415UnsupportedMediaType,
            code,
            title);

    public static IResult UnprocessableEntity(
        this HttpContext context,
        string code = "UNPROCESSABLE_ENTITY",
        string title = "The request could not be processed.")
        => Create(context, StatusCodes.Status422UnprocessableEntity, code, title);

    public static IResult TooManyRequests(
        this HttpContext context,
        string code = "TOO_MANY_REQUESTS",
        string title = "Too many requests.")
        => Create(context, StatusCodes.Status429TooManyRequests, code, title);

    public static IResult InternalServerError(
        this HttpContext context,
        string code = "INTERNAL_SERVER_ERROR",
        string title = "An unexpected error occurred.")
        => Create(context, StatusCodes.Status500InternalServerError, code, title);

    public static IResult ServiceUnavailable(
        this HttpContext context,
        string code = "SERVICE_UNAVAILABLE",
        string title = "The service is temporarily unavailable.")
        => Create(context, StatusCodes.Status503ServiceUnavailable, code, title);

    public static IResult ValidationProblem(
        this HttpContext context,
        IDictionary<string, string[]> errors,
        string code = "VALIDATION_ERROR",
        string title = "One or more validation errors occurred.")
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = title,
            Type = GetProblemType(StatusCodes.Status400BadRequest)
        };

        problem.AddMetadata(context, code);

        return Results.Problem(problem);
    }

    public static IResult ValidationFailed(
        this HttpContext context,
        IReadOnlyList<ValidationIssue> issues,
        string code = "VALIDATION_FAILED",
        string title = "Validation failed.")
    {
        var errors = issues
            .GroupBy(x => x.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(x => x.Message).ToArray());

        return context.ValidationProblem(errors, code, title);
    }

    public static IResult FromDiagnostic(
        this HttpContext context,
        Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        var status = MapStatusCode(diagnostic.Kind);
        var code = diagnostic.Code.ToString();
        var title = diagnostic.Message ?? "An error occurred.";

        if (diagnostic.Kind == DiagnosticKind.Validation &&
            diagnostic.Message is not null)
        {
            return context.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["request"] = [diagnostic.Message]
                },
                code);
        }

        return Create(context, status, code, title);
    }

    private static IResult Create(
        HttpContext context,
        int status,
        string code,
        string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = GetProblemType(status)
        };

        problem.AddMetadata(context, code);

        return Results.Problem(problem);
    }

    private static int MapStatusCode(DiagnosticKind kind)
        => kind switch
        {
            DiagnosticKind.Validation =>
                StatusCodes.Status400BadRequest,

            DiagnosticKind.NotFound =>
                StatusCodes.Status404NotFound,

            DiagnosticKind.Conflict =>
                StatusCodes.Status409Conflict,

            DiagnosticKind.Unauthorized =>
                StatusCodes.Status401Unauthorized,

            DiagnosticKind.Forbidden =>
                StatusCodes.Status403Forbidden,

            _ => StatusCodes.Status500InternalServerError
        };

    private static string GetProblemType(int status)
        => $"https://httpstatuses.com/{status}";

    private static void AddMetadata(
        this ProblemDetails problem,
        HttpContext context,
        string code)
    {
        problem.Extensions["errorCode"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["correlationId"] =
            context.Items.TryGetValue("CorrelationId", out var value)
                ? value?.ToString()
                : context.TraceIdentifier;
    }
}