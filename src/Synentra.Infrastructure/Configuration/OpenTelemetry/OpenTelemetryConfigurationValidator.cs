using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.OpenTelemetry;

public sealed class OpenTelemetryConfigurationValidator
    : IValidateOptions<OpenTelemetryConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        OpenTelemetryConfiguration options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var errors = new List<string>();

        ValidateServiceName(options.ServiceName, errors);
        ValidateEndpoint(options.Endpoint, errors);
        ValidateExportSignals(options, errors);

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateServiceName(
        string? serviceName,
        List<string> errors)
    {
        const string property = "OpenTelemetry:ServiceName";

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            errors.Add($"{property} is required.");
            return;
        }

        if (serviceName.Length > 255)
        {
            errors.Add(
                $"{property} must not exceed 255 characters.");
        }
    }

    private static void ValidateEndpoint(
        string? endpoint,
        List<string> errors)
    {
        const string property = "OpenTelemetry:Endpoint";

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            errors.Add($"{property} is required.");
            return;
        }

        if (!Uri.TryCreate(
                endpoint,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            errors.Add(
                $"{property} must be a valid HTTP or HTTPS URL " +
                "without embedded credentials or fragments.");
        }
    }

    private static void ValidateExportSignals(
        OpenTelemetryConfiguration options,
        List<string> errors)
    {
        if (options.ExportLogs != true &&
            options.ExportTraces != true &&
            options.ExportMetrics != true)
        {
            errors.Add(
                "OpenTelemetry: At least one export signal " +
                "(Logs, Traces, or Metrics) must be enabled.");
        }
    }
}