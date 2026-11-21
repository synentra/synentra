using Synentra.Domain.Common.Results;

namespace Synentra.Endpoints.V2.Support;

internal static class EndpointResultExtensions
{
    public static IResult ToHttpResult<T>(
        this PaginatedResult<T> result,
        HttpContext context)
    {
        context.Response.Headers["X-Page-Number"] =
            result.PageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Response.Headers["X-Page-Size"] =
            result.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Response.Headers["X-Total-Count"] =
            result.TotalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

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

        return ApiProblemDetailsExtensions.FromDiagnostic(context, result.Diagnostic!);
    }

    public static IResult ToHttpResult<T>(
        this Result<T> result,
        HttpContext context)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return ApiProblemDetailsExtensions.FromDiagnostic(context, result.Diagnostic!);
    }

    public static IResult ToHttpResult(this Result result, HttpContext context)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return ApiProblemDetailsExtensions.FromDiagnostic(context, result.Diagnostic!);
    }
}
