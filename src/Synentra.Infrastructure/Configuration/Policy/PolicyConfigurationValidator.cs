using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Policy;

public sealed class PolicyConfigurationValidator
    : IValidateOptions<PolicyConfiguration>
{
    private const string DeclarativeProvider = "Declarative";
    private const string OpaProvider = "Opa";

    public ValidateOptionsResult Validate(
        string? name,
        PolicyConfiguration options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.DefaultProvider))
        {
            errors.Add("Policy:DefaultProvider is required.");
        }
        else if (!IsSupportedProvider(options.DefaultProvider))
        {
            errors.Add("Policy:DefaultProvider must be 'Declarative' or 'Opa'.");
        }

        if (options.DefaultProvider?.Equals(
                OpaProvider,
                StringComparison.OrdinalIgnoreCase) == true)
        {
            if (options.Providers?.Opa is null)
            {
                errors.Add("Policy:Providers:Opa is required when Opa is selected.");
            }
            else
            {
                ValidateOpa(options.Providers.Opa, errors);
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static bool IsSupportedProvider(string provider)
    {
        return provider.Equals(DeclarativeProvider, StringComparison.OrdinalIgnoreCase)
            || provider.Equals(OpaProvider, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateOpa(
        OpaPolicyConfiguration options,
        List<string> errors)
    {
        ValidateBaseUrl(options.BaseUrl, errors);
        ValidatePath(options.Path, errors);
        ValidateTimeout(options.Timeout, errors);
    }

    private static void ValidateBaseUrl(
        string? baseUrl,
        List<string> errors)
    {
        const string property = "Policy:Providers:Opa:BaseUrl";

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            errors.Add($"{property} is required.");
            return;
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            uri.AbsolutePath != "/")
        {
            errors.Add(
                $"{property} must be a valid HTTP or HTTPS origin " +
                "without credentials, path, query, or fragment.");
        }
    }

    private static void ValidatePath(
        string? path,
        List<string> errors)
    {
        const string property = "Policy:Providers:Opa:Path";

        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{property} is required.");
            return;
        }

        if (!path.StartsWith('/') ||
            path.StartsWith("//", StringComparison.Ordinal) ||
            path.Contains('?') ||
            path.Contains('#') ||
            path.Contains('\\') ||
            path.Any(char.IsWhiteSpace) ||
            Uri.TryCreate(path, UriKind.Absolute, out _))
        {
            errors.Add($"{property} must be a valid root-relative URL path starting with '/'.");
        }
    }

    private static void ValidateTimeout(
        TimeSpan? timeout,
        List<string> errors)
    {
        const string property = "Policy:Providers:Opa:Timeout";

        if (timeout is null)
            return;

        if (timeout <= TimeSpan.Zero)
        {
            errors.Add($"{property} must be greater than zero.");
        }
        else if (timeout > TimeSpan.FromMinutes(5))
        {
            errors.Add($"{property} must not exceed 5 minutes.");
        }
    }
}
