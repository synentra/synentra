using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Cors;

public sealed class CorsConfigurationValidator : IValidateOptions<CorsConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        CorsConfiguration options)
    {
        var errors = new List<string>();

        if (options.AllowedOrigins is null || options.AllowedOrigins.Length == 0)
        {
            return ValidateOptionsResult.Success;
        }

        ValidateOrigins(options.AllowedOrigins, errors);
        ValidateTokens(
            options.AllowedMethods,
            nameof(options.AllowedMethods),
            errors);

        ValidateTokens(
            options.AllowedHeaders,
            nameof(options.AllowedHeaders),
            errors);

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateOrigins(
        string[] origins,
        List<string> errors)
    {
        const string property = "Cors:AllowedOrigins";

        if (origins.Contains("*"))
        {
            errors.Add(
                $"{property} must not contain wildcard '*'. " +
                "Explicitly configure trusted origins.");
        }

        foreach (var origin in origins)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                errors.Add($"{property} contains an empty origin.");
                continue;
            }

            if (!Uri.TryCreate(
                    origin,
                    UriKind.Absolute,
                    out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrEmpty(uri.Host) ||
                uri.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment) ||
                !string.Equals(
                    origin,
                    uri.GetLeftPart(UriPartial.Authority),
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"{property} contains an invalid origin: '{origin}'.");
            }
        }

        ValidateDuplicates(origins, property, errors);
    }

    private static void ValidateTokens(
        string[]? values,
        string propertyName,
        List<string> errors)
    {
        var property = $"Cors:{propertyName}";

        if (values is null || values.Length == 0)
        {
            return;
        }

        if (values.Contains("*"))
        {
            if (values.Length > 1)
            {
                errors.Add(
                    $"{property} cannot combine '*' with other values.");
            }

            return;
        }

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !IsValidHttpToken(value))
            {
                errors.Add(
                    $"{property} contains an invalid HTTP token: '{value}'.");
            }
        }

        ValidateDuplicates(values, property, errors);
    }

    private static bool IsValidHttpToken(string value)
    {
        const string symbols = "!#$%&'*+-.^_`|~";

        return value.Length > 0 &&
               value.All(c =>
                   c is >= 'A' and <= 'Z' ||
                   c is >= 'a' and <= 'z' ||
                   c is >= '0' and <= '9' ||
                   symbols.Contains(c));
    }

    private static void ValidateDuplicates(
        string[] values,
        string property,
        List<string> errors)
    {
        var duplicates = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicate in duplicates)
        {
            errors.Add(
                $"{property} contains duplicate value: '{duplicate}'.");
        }
    }
}