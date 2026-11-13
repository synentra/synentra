using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Intent;

public sealed class IntentConfigurationValidator : IValidateOptions<IntentConfiguration>
{
    public ValidateOptionsResult Validate(string? name, IntentConfiguration options)
    {
        var errors = new List<string>();

        if (options.Providers is null)
        {
            errors.Add("Intent:Providers is required.");
        }
        else
        {
            ValidateInternal(options.Providers.Internal, errors);
            ValidateAzureAi(options.Providers.AzureAi, errors);
            ValidateOpenAi(options.Providers.OpenAi, errors);
            ValidateGemini(options.Providers.Gemini, errors);
            ValidateOllama(options.Providers.Ollama, errors);
        }

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }

    private static void ValidateInternal(InternalOnnxConfiguration? options, List<string> errors)
    {
        const string prefix = "Intent:Providers:Internal";

        if (options is not null && !options.Enabled)
            return;

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        var isCommunity = string.Equals(options.ModelType, "Community", StringComparison.OrdinalIgnoreCase);
        var isPro = string.Equals(options.ModelType, "Pro", StringComparison.OrdinalIgnoreCase);

        if (!isCommunity && !isPro)
            errors.Add($"{prefix}:ModelType must be 'Community' or 'Pro'.");
        
        ValidateAbsolutePath(options.PackagePath, $"{prefix}:PackagePath", errors);

        if (isPro)
            ValidateAbsolutePath(options.LicensePath, $"{prefix}:LicensePath", errors);
    }

    private static void ValidateAzureAi(AzureAiConfiguration? options, List<string> errors)
    {
        const string prefix = "Intent:Providers:AzureAi";

        if (options is not null && !options.Enabled)
            return;

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        ValidateEndpoint(options.Endpoint, $"{prefix}:Endpoint", errors, requireHttps: true);

        Require(options.ApiKey, $"{prefix}:ApiKey", errors);
        Require(options.Model, $"{prefix}:Model", errors);

        ValidateTemperature(options.Temperature, $"{prefix}:Temperature", errors);
        ValidateMaxTokens(options.MaxTokens, $"{prefix}:MaxTokens", errors);
    }

    private static void ValidateOpenAi(OpenAiConfiguration? options, List<string> errors)
    {
        const string prefix = "Intent:Providers:OpenAi";

        if (options is not null && !options.Enabled)
            return;

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        Require(options.ApiKey, $"{prefix}:ApiKey", errors);
        Require(options.Model, $"{prefix}:Model", errors);

        ValidateTemperature(options.Temperature, $"{prefix}:Temperature", errors);
        ValidateMaxTokens(options.MaxTokens, $"{prefix}:MaxTokens", errors);
    }

    private static void ValidateGemini(GeminiConfiguration? options, List<string> errors)
    {
        const string prefix = "Intent:Providers:Gemini";

        if (options is not null && !options.Enabled)
            return;

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        Require(options.ApiKey, $"{prefix}:ApiKey", errors);
        Require(options.Model, $"{prefix}:Model", errors);

        ValidateTemperature(options.Temperature, $"{prefix}:Temperature", errors);
        ValidateMaxTokens(options.MaxTokens, $"{prefix}:MaxTokens", errors);
    }

    private static void ValidateOllama(OllamaConfiguration? options, List<string> errors)
    {
        const string prefix = "Intent:Providers:Ollama";

        if (options is not null && !options.Enabled)
            return;

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        ValidateEndpoint(options.Endpoint, $"{prefix}:Endpoint", errors, requireHttps: false);
        Require(options.Model, $"{prefix}:Model", errors);
    }

    private static void ValidateTemperature(double? temperature, string property, List<string> errors)
    {
        if (temperature is null)
            return;

        if (!double.IsFinite(temperature.Value) || temperature is < 0 or > 2)
            errors.Add($"{property} must be a finite number between 0 and 2.");
    }

    private static void ValidateMaxTokens(int? maxTokens, string property, List<string> errors)
    {
        if (maxTokens is <= 0)
            errors.Add($"{property} must be greater than zero.");
    }

    private static void ValidateAbsolutePath(string? path, string property, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{property} is required.");
            return;
        }
    }

    private static void ValidateEndpoint(string? endpoint, string property, List<string> errors, bool requireHttps)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            errors.Add($"{property} is required.");
            return;
        }

        if (!Uri.TryCreate(
                endpoint,
                UriKind.Absolute,
                out var uri) ||
            (requireHttps
                ? uri.Scheme != Uri.UriSchemeHttps
                : uri.Scheme != Uri.UriSchemeHttp &&
                  uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            errors.Add(
                $"{property} must be a valid " +
                (requireHttps ? "HTTPS" : "HTTP or HTTPS") +
                " URL without embedded credentials, " +
                "query, or fragment.");
        }
    }

    private static void Require(string? value, string property, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{property} is required.");
    }
}