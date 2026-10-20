using System.Text.Json;

namespace Synentra.Infrastructure.Features.Adapters;

internal static class AdapterConfiguration
{
    public static string? String(IReadOnlyDictionary<string, object> configuration, string key)
    {
        if (!configuration.TryGetValue(key, out var value) || value is null)
            return null;

        if (value is string s)
            return s;

        if (value is JsonElement element)
            return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    public static IReadOnlyDictionary<string, string> StringMap(IReadOnlyDictionary<string, object> configuration, string key)
    {
        if (!configuration.TryGetValue(key, out var value) || value is null)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            return element.EnumerateObject().ToDictionary(
                x => x.Name,
                x => x.Value.ValueKind == JsonValueKind.String ? x.Value.GetString() ?? string.Empty : x.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);
        }

        if (value is IReadOnlyDictionary<string, object> readOnly)
            return readOnly.ToDictionary(x => x.Key, x => Convert.ToString(x.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        if (value is IDictionary<object, object> yaml)
            return yaml.ToDictionary(x => Convert.ToString(x.Key) ?? string.Empty, x => Convert.ToString(x.Value) ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public static string BuildAbsoluteEndpoint(string endpoint, string? path)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("Target endpoint must be an absolute URI.");

        if (string.IsNullOrWhiteSpace(path))
            return baseUri.ToString();

        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
        {
            if (!string.Equals(absolute.GetLeftPart(UriPartial.Authority), baseUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Binding path cannot override the registered target origin.");
            return absolute.ToString();
        }

        return new Uri(baseUri, path).ToString();
    }
}
