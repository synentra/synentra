using Synentra.Endpoints.V2;

namespace Synentra.Middleware;

public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresIdempotencyKey(context.Request))
        {
            var key = ApiEndpointSupport.GetIdempotencyKey(context.Request);
            if (string.IsNullOrWhiteSpace(key))
                throw new IdempotencyApiException("Idempotency-Key is required for mutating requests.");

            if (key.Length > 200)
                throw new IdempotencyApiException("Idempotency-Key must not exceed 200 characters.");

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
}
