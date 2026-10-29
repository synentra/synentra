namespace Synentra.Infrastructure.Configuration.Security;

public class IdentityConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Identity";

    public string DefaultProvider { get; set; } = "oidc";
    public IdentityProviderConfiguration Providers { get; set; } = new();
}