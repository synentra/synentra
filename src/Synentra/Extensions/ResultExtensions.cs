using Microsoft.AspNetCore.Mvc;
using Synentra.Domain.Common.Diagnostics;
using Synentra.Domain.Common.Results;

namespace Synentra.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this PaginatedResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(new
            {
                result.Items,
                result.PageNumber,
                result.PageSize,
                result.TotalCount,
                result.TotalPages,
                result.HasPreviousPage,
                result.HasNextPage
            });
        }

        return MapError(result.Diagnostic!);
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return MapError(result.Diagnostic!);
    }

    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return MapError(result.Diagnostic!);
    }

    private static IResult MapError(Diagnostic diagnostic)
    {
        var status = diagnostic.Kind switch
        {
            DiagnosticKind.Validation => StatusCodes.Status400BadRequest,
            DiagnosticKind.NotFound => StatusCodes.Status404NotFound,
            DiagnosticKind.Conflict => StatusCodes.Status409Conflict,
            DiagnosticKind.Unauthorized => StatusCodes.Status401Unauthorized,
            DiagnosticKind.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        if (diagnostic.Kind == DiagnosticKind.Validation && diagnostic.Message is not null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Error", new[] { diagnostic.Message } }
            });
        }

        var problem = new ProblemDetails
        {
            Title = diagnostic.Message,
            Status = status,
            Type = $"https://httpstatuses.com/{status}"
        };

        problem.Extensions["errorCode"] = diagnostic.Code.ToString();

        return Results.Problem(problem);
    }
}