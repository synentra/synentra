using Synentra.Endpoints.V2.Diagnostics;
using Synentra.Exceptions;

namespace Synentra.Middleware;

public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresIdempotencyKey(context.Request))
        {
            var key = GetIdempotencyKey(context.Request);
            if (string.IsNullOrWhiteSpace(key))
                throw new IdempotencyApiException(EndpointsError.Idempotency.IdempotencyKeyRequired);

            if (key.Length > 200)
                throw new IdempotencyApiException(EndpointsError.Idempotency.IdempotencyKeyTooLong);

            context.Items["IdempotencyKey"] = key;
            context.Response.Headers["Idempotency-Key"] = key;
        }

        await next(context);
    }

    private static bool RequiresIdempotencyKey(HttpRequest request)
    {
        if (!request.Path.StartsWithSegments("/v2") || request.Method is "GET" or "HEAD" or "OPTIONS")
            return false;

        if (request.Method == "POST" && (request.Path.Equals("/v2/simulate") || request.Path.Equals("/v2/manifests/validate")))
            return false;

        return request.Method is "POST" or "PUT" or "DELETE";
    }

    private static string? GetIdempotencyKey(HttpRequest request) 
        => request.Headers.TryGetValue("Idempotency-Key", out var value) && !string.IsNullOrWhiteSpace(value)
        ? value.ToString()
        : null;
}
