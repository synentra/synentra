namespace Synentra.Endpoints.V2.Support;

internal static class EndpointRequestExtensions
{
    public static string? GetIdempotencyKey(this HttpRequest request)
        => request.Headers.TryGetValue("Idempotency-Key", out var value) &&
           !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : null;

    public static bool IsSupportedManifestMediaType(this HttpRequest request)
        => IsSupportedManifestMediaType(request.ContentType);

    public static bool IsSupportedManifestMediaType(this string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return true;

        var mediaType = contentType.Split(';', 2)[0].Trim();

        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/yaml", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("text/yaml", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/x-yaml", StringComparison.OrdinalIgnoreCase);
    }
}
