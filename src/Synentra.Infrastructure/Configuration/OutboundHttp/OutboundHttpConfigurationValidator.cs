using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.OutboundHttp;

public sealed class OutboundHttpConfigurationValidator
    : IValidateOptions<OutboundHttpConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        OutboundHttpConfiguration options)
    {
        var errors = new List<string>();

        ValidateAllowedDestinations(
            options.AllowedDestinations,
            errors);

        ValidateAllowPrivateAddresses(
            options.AllowPrivateAddresses,
            errors);

        ValidateMaxResponseBodySize(
            options.MaxResponseBodySizeBytes,
            errors);

        ValidateTimeout(
            options.Timeout,
            errors);

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateAllowedDestinations(
        string[]? destinations,
        List<string> errors)
    {
        const string property = "OutboundHttp:AllowedDestinations";

        if (destinations is null || destinations.Length == 0)
        {
            return;
        }

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var destination in destinations)
        {
            if (string.IsNullOrWhiteSpace(destination))
            {
                errors.Add($"{property} contains an empty destination.");
                continue;
            }

            if (destination.Contains('*'))
            {
                errors.Add(
                    $"{property} does not allow wildcard destinations: " +
                    $"'{destination}'.");
                continue;
            }

            if (!Uri.TryCreate(
                    destination,
                    UriKind.Absolute,
                    out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrWhiteSpace(uri.Host) ||
                uri.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment) ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.Equals(
                    destination,
                    uri.GetLeftPart(UriPartial.Authority),
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"{property} contains an invalid destination: " +
                    $"'{destination}'.");
                continue;
            }

            var normalized = uri.GetLeftPart(UriPartial.Authority);

            if (!seen.Add(normalized))
            {
                errors.Add(
                    $"{property} contains duplicate destination: " +
                    $"'{destination}'.");
            }
        }
    }

    private static void ValidateAllowPrivateAddresses(
        bool? allowPrivateAddresses,
        List<string> errors)
    {
        if (allowPrivateAddresses is null)
        {
            errors.Add(
                "OutboundHttp:AllowPrivateAddresses must be " +
                "explicitly true or false.");
        }
    }

    private static void ValidateMaxResponseBodySize(
        int? maxResponseBodySizeBytes,
        List<string> errors)
    {
        const string property = "OutboundHttp:MaxResponseBodySizeBytes";

        if (maxResponseBodySizeBytes is null)
            return;

        if (maxResponseBodySizeBytes <= 0)
        {
            errors.Add($"{property} must be greater than zero.");
        }
    }

    private static void ValidateTimeout(
        TimeSpan? timeout,
        List<string> errors)
    {
        const string property = "OutboundHttp:Timeout";

        if (timeout is null)
            return;

        if (timeout <= TimeSpan.Zero)
        {
            errors.Add($"{property} must be greater than zero.");
        }

        if (timeout > TimeSpan.FromMinutes(10))
        {
            errors.Add(
                $"{property} must not exceed 10 minutes.");
        }
    }
}