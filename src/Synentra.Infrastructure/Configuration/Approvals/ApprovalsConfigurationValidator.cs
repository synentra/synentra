using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Approvals;

public sealed class ApprovalsConfigurationValidator : IValidateOptions<ApprovalsConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        ApprovalsConfiguration options)
    {
        if (options.MaxManualTrustAdjustment < 0)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.MaxManualTrustAdjustment)} must be greater than or equal to 0.");
        }

        return ValidateOptionsResult.Success;
    }
}