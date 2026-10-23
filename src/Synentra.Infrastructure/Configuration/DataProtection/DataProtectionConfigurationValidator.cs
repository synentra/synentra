using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.DataProtection;

public sealed class DataProtectionConfigurationValidator
    : IValidateOptions<DataProtectionConfiguration>
{
    private static readonly TimeSpan MinKeyLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan MaxKeyLifetime = TimeSpan.FromDays(365);

    public ValidateOptionsResult Validate(
        string? name,
        DataProtectionConfiguration options)
    {
        var errors = new List<string>();

        ValidateApplicationName(
            options.ApplicationName,
            errors);

        ValidateKeyRingPath(
            options.KeyRingPath,
            errors);

        ValidateDefaultKeyLifetime(
            options.DefaultKeyLifetime,
            errors);

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateApplicationName(
        string? applicationName,
        List<string> errors)
    {
        const string property = "DataProtection:ApplicationName";

        if (string.IsNullOrWhiteSpace(applicationName))
        {
            errors.Add($"{property} is required.");
            return;
        }

        if (applicationName.Length > 255)
        {
            errors.Add($"{property} must not exceed 255 characters.");
        }
    }

    private static void ValidateKeyRingPath(
        string? keyRingPath,
        List<string> errors)
    {
        const string property = "DataProtection:KeyRingPath";

        if (string.IsNullOrWhiteSpace(keyRingPath))
        {
            errors.Add($"{property} is required.");
            return;
        }

        if (keyRingPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            errors.Add($"{property} contains invalid path characters.");
            return;
        }

        try
        {
            _ = Path.GetFullPath(keyRingPath);
        }
        catch (Exception ex) when (
            ex is ArgumentException or
            NotSupportedException or
            PathTooLongException)
        {
            errors.Add($"{property} must be a valid filesystem path.");
        }
    }

    private static void ValidateDefaultKeyLifetime(
        TimeSpan? lifetime,
        List<string> errors)
    {
        const string property = "DataProtection:DefaultKeyLifetime";

        if (lifetime is null)
        {
            errors.Add($"{property} is required.");
            return;
        }

        if (lifetime < MinKeyLifetime ||
            lifetime > MaxKeyLifetime)
        {
            errors.Add($"{property} must be between {MinKeyLifetime.TotalDays:0} and {MaxKeyLifetime.TotalDays:0} days.");
        }
    }
}