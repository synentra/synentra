namespace Synentra.Infrastructure.Configuration.Identity;

public class IdentityConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Identity";

    public string Provider { get; set; } = "oidc";
    public IdentityClaimConfiguration Claims { get; set; } = new();
    public IdentityProviderConfiguration Providers { get; set; } = new();
}