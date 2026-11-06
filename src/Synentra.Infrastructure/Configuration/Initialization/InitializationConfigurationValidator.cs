using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Initialization;

public sealed class InitializationConfigurationValidator : IValidateOptions<InitializationConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        InitializationConfiguration options)
    {
        var errors = new List<string>();

        ValidateInitializationConfiguration(options, errors);

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }

    private static void ValidateInitializationConfiguration(
        InitializationConfiguration initialization,
        List<string> errors)
    {
        if (initialization.Enabled.HasValue && !initialization.Enabled.Value)
        {
            return;
        }

        if (initialization.PollingInterval.HasValue && initialization.PollingInterval.Value <= TimeSpan.Zero)
        {
            errors.Add("Initialization.PollingInterval must be greater than zero.");
        }
    }
}