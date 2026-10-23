namespace Synentra.Infrastructure.Configuration.SecretManagement;

public class SecretManagementConfiguration: ISynentraConfiguration
{
    public static string SectionName => "SecretManagement";

    public SecretManagementProviderType DefaultProvider { get; set; } = SecretManagementProviderType.None;
    public SecretManagementProviders Providers { get; set; } = new();
}