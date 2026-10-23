using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Trust;

public sealed class TrustConfigurationValidator : IValidateOptions<TrustConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        TrustConfiguration options)
    {
        if (options.InitialScore is < 0 or > 100)
        {
            return ValidateOptionsResult.Fail(
                "InitialScore must be between 0 and 100.");
        }

        if (options.QuarantineThreshold is < 0 or > 100)
        {
            return ValidateOptionsResult.Fail(
                "QuarantineThreshold must be between 0 and 100.");
        }

        return ValidateOptionsResult.Success;
    }
}