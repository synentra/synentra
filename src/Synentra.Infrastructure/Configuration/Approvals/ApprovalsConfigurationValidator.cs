using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Approvals;

public sealed class ApprovalsConfigurationValidator : IValidateOptions<ApprovalsConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        ApprovalsConfiguration options)
    {
        return ValidateOptionsResult.Success;
    }
}