namespace Synentra.Infrastructure.Configuration.DataProtection;

public class DataProtectionConfiguration
{
    public string? ApplicationName { get; set; } = "synentra";
    public string? KeyRingPath { get; set; } = "./data/keys";
    public TimeSpan? DefaultKeyLifetime { get; set; } = TimeSpan.FromDays(90);
}
