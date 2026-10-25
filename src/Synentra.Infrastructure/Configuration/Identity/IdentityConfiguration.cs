namespace Synentra.Infrastructure.Configuration.Security;

public class IdentityConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Identity";

    public string DefaultProvider { get; set; } = "local";
    public IdentityProviderConfiguration Providers { get; set; } = new();
}