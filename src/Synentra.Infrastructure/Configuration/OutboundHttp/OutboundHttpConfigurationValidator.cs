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