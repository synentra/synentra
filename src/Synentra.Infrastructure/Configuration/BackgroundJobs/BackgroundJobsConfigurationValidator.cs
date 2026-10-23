using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.BackgroundJobs;

public sealed class BackgroundJobsConfigurationValidator : IValidateOptions<BackgroundJobsConfiguration>
{
    public ValidateOptionsResult Validate(
        string? name,
        BackgroundJobsConfiguration options)
    {
        var errors = new List<string>();

        if (options.Outbox is not null)
        {
            ValidateOutboxConfiguration(options.Outbox, errors);
            ValidateClaimExpirationConfiguration(options.ClaimExpiration, errors);
            ValidateInitializationConfiguration(options.Initialization, errors);
        }

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }

    private static void ValidateOutboxConfiguration(
        OutboxConfiguration outbox,
        List<string> errors)
    {
        if (outbox.Enabled.HasValue && !outbox.Enabled.Value)
        {
            return;
        }

        if (outbox.PollingInterval.HasValue && outbox.PollingInterval.Value <= TimeSpan.Zero)
        {
            errors.Add("Outbox.PollingInterval must be greater than zero.");
        }

        if (outbox.MaxAttempts.HasValue && outbox.MaxAttempts.Value <= 0)
        {
            errors.Add("Outbox.MaxAttempts must be greater than zero.");
        }
    }

    private static void ValidateClaimExpirationConfiguration(
        ClaimExpirationConfiguration claimExpiration,
        List<string> errors)
    {
        if (claimExpiration.Enabled.HasValue && !claimExpiration.Enabled.Value)
        {
            return;
        }

        if (claimExpiration.PollingInterval.HasValue && claimExpiration.PollingInterval.Value <= TimeSpan.Zero)
        {
            errors.Add("ClaimExpiration.PollingInterval must be greater than zero.");
        }
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