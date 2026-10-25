namespace Synentra.Infrastructure.Configuration.Secrets;

public class SecretsConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Secrets";

    public string DefaultProvider { get; set; } = "none";
    public SecretProviders Providers { get; set; } = new();
}