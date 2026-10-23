using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace Synentra.Infrastructure.Configuration.Logging;

public sealed class LoggingConfigurationValidator
    : IValidateOptions<LoggingConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        LoggingConfiguration options)
    {
        var errors = new List<string>();

        ValidateLogLevel(
            options.DefaultLogLevel,
            "Logging:DefaultLogLevel",
            errors);

        if (options.File is { Enabled: true } file)
            ValidateFile(file, errors);

        if (options.Seq is { Enabled: true } seq)
            ValidateSeq(seq, errors);

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateFile(
        FileLoggingConfiguration options,
        List<string> errors)
    {
        const string prefix = "Logging:File";

        ValidateLogLevel(
            options.LogLevel,
            $"{prefix}:LogLevel",
            errors);

        Require(
            options.LogPath,
            $"{prefix}:LogPath",
            errors);

        if (!string.IsNullOrWhiteSpace(options.RollingInterval) &&
            !TryParseEnum<RollingInterval>(options.RollingInterval))
        {
            errors.Add(
                $"{prefix}:RollingInterval must be a valid " +
                "Serilog rolling interval.");
        }

        if (options.RetainedFileCountLimit is <= 0)
        {
            errors.Add(
                $"{prefix}:RetainedFileCountLimit must be greater than zero.");
        }
    }

    private static void ValidateSeq(
        SeqLoggingConfiguration options,
        List<string> errors)
    {
        const string prefix = "Logging:Seq";

        ValidateLogLevel(
            options.LogLevel,
            $"{prefix}:LogLevel",
            errors);

        Require(
            options.Url,
            $"{prefix}:Url",
            errors);

        if (!string.IsNullOrWhiteSpace(options.Url) &&
            (!Uri.TryCreate(
                options.Url,
                UriKind.Absolute,
                out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp &&
              uri.Scheme != Uri.UriSchemeHttps) ||
             string.IsNullOrWhiteSpace(uri.Host) ||
             !string.IsNullOrEmpty(uri.UserInfo)))
        {
            errors.Add(
                $"{prefix}:Url must be a valid HTTP or HTTPS URL " +
                "without embedded credentials.");
        }

        // ApiKey is optional.
    }

    private static void ValidateLogLevel(
        string? value,
        string property,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{property} is required.");
            return;
        }

        if (!TryParseEnum<LogLevel>(value))
        {
            errors.Add(
                $"{property} must be a valid log level.");
        }
    }

    private static bool TryParseEnum<TEnum>(string value)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(
                   value,
                   ignoreCase: true,
                   out var result)
               && Enum.IsDefined(result);
    }

    private static void Require(
        string? value,
        string property,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{property} is required.");
    }
}