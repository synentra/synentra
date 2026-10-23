using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Semantic;

public sealed class SemanticConfigurationValidator
    : IValidateOptions<SemanticConfiguration>
{
    private const string InternalProvider = "Internal";
    private const string AzureAiProvider = "AzureAi";
    private const string OpenAiProvider = "OpenAi";
    private const string GeminiProvider = "Gemini";
    private const string OllamaProvider = "Ollama";

    public ValidateOptionsResult Validate(
        string? name,
        SemanticConfiguration options)
    {
        if (options.Enabled == false)
            return ValidateOptionsResult.Success;

        var errors = new List<string>();

        ValidateConfidenceThreshold(
            options.ConfidenceThreshold,
            errors);

        if (string.IsNullOrWhiteSpace(options.DefaultProvider))
        {
            errors.Add("Semantic:DefaultProvider is required.");
        }
        else if (options.Providers is null)
        {
            errors.Add("Semantic:Providers is required.");
        }
        else
        {
            switch (options.DefaultProvider.ToUpperInvariant())
            {
                case "INTERNAL":
                    ValidateInternal(options.Providers.Internal, errors);
                    break;

                case "AZUREAI":
                    ValidateAzureAi(options.Providers.AzureAi, errors);
                    break;

                case "OPENAI":
                    ValidateOpenAi(options.Providers.OpenAi, errors);
                    break;

                case "GEMINI":
                    ValidateGemini(options.Providers.Gemini, errors);
                    break;

                case "OLLAMA":
                    ValidateOllama(options.Providers.Ollama, errors);
                    break;

                default:
                    errors.Add(
                        "Semantic:DefaultProvider must be one of: " +
                        "Internal, AzureAi, OpenAi, Gemini, Ollama.");
                    break;
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateConfidenceThreshold(
        double? threshold,
        List<string> errors)
    {
        const string property = "Semantic:ConfidenceThreshold";

        if (threshold is null ||
            !double.IsFinite(threshold.Value) ||
            threshold is < 0 or > 1)
        {
            errors.Add(
                $"{property} must be a finite number between 0 and 1.");
        }
    }

    private static void ValidateInternal(
        InternalOnnxConfiguration? options,
        List<string> errors)
    {
        const string prefix = "Semantic:Providers:Internal";

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        var isCommunity = string.Equals(
            options.ModelType,
            "Community",
            StringComparison.OrdinalIgnoreCase);

        var isPro = string.Equals(
            options.ModelType,
            "Pro",
            StringComparison.OrdinalIgnoreCase);

        if (!isCommunity && !isPro)
        {
            errors.Add(
                $"{prefix}:ModelType must be 'Community' or 'Pro'.");
        }

        ValidateAbsolutePath(
            options.PackagePath,
            $"{prefix}:PackagePath",
            errors);

        if (isPro)
        {
            ValidateAbsolutePath(
                options.LicensePath,
                $"{prefix}:LicensePath",
                errors);
        }
    }

    private static void ValidateAzureAi(
        AzureAiConfiguration? options,
        List<string> errors)
    {
        const string prefix = "Semantic:Providers:AzureAi";

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        ValidateEndpoint(
            options.Endpoint,
            $"{prefix}:Endpoint",
            errors,
            requireHttps: true);

        Require(options.ApiKey, $"{prefix}:ApiKey", errors);
        Require(options.Model, $"{prefix}:Model", errors);

        ValidateTemperature(
            options.Temperature,
            $"{prefix}:Temperature",
            errors);

        ValidateMaxTokens(
            options.MaxTokens,
            $"{prefix}:MaxTokens",
            errors);
    }

    private static void ValidateOpenAi(
        OpenAiConfiguration? options,
        List<string> errors)
    {
        const string prefix = "Semantic:Providers:OpenAi";

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        Require(options.ApiKey, $"{prefix}:ApiKey", errors);
        Require(options.Model, $"{prefix}:Model", errors);

        ValidateTemperature(
            options.Temperature,
            $"{prefix}:Temperature",
            errors);

        ValidateMaxTokens(
            options.MaxTokens,
            $"{prefix}:MaxTokens",
            errors);
    }

    private static void ValidateGemini(
        GeminiConfiguration? options,
        List<string> errors)
    {
        const string prefix = "Semantic:Providers:Gemini";

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        Require(options.ApiKey, $"{prefix}:ApiKey", errors);
        Require(options.Model, $"{prefix}:Model", errors);

        ValidateTemperature(
            options.Temperature,
            $"{prefix}:Temperature",
            errors);

        ValidateMaxTokens(
            options.MaxTokens,
            $"{prefix}:MaxTokens",
            errors);
    }

    private static void ValidateOllama(
        OllamaConfiguration? options,
        List<string> errors)
    {
        const string prefix = "Semantic:Providers:Ollama";

        if (options is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        ValidateEndpoint(
            options.Endpoint,
            $"{prefix}:Endpoint",
            errors,
            requireHttps: false);

        Require(options.Model, $"{prefix}:Model", errors);
    }

    private static void ValidateTemperature(
        double? temperature,
        string property,
        List<string> errors)
    {
        if (temperature is null)
            return;

        if (!double.IsFinite(temperature.Value) ||
            temperature is < 0 or > 2)
        {
            errors.Add(
                $"{property} must be a finite number between 0 and 2.");
        }
    }

    private static void ValidateMaxTokens(
        int? maxTokens,
        string property,
        List<string> errors)
    {
        if (maxTokens is <= 0)
        {
            errors.Add(
                $"{property} must be greater than zero.");
        }
    }

    private static void ValidateAbsolutePath(
        string? path,
        string property,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{property} is required.");
            return;
        }
    }

    private static void ValidateEndpoint(
        string? endpoint,
        string property,
        List<string> errors,
        bool requireHttps)
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

    private static void Require(
        string? value,
        string property,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{property} is required.");
    }
}