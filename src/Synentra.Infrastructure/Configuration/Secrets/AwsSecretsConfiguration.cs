namespace Synentra.Infrastructure.Configuration.Secrets;

public class AwsSecretsConfiguration
{
    public string? AwsRegion { get; set; }
    public string AwsIdentity { get; set; } = "web-identity";
    public string? AwsAccessKey { get; set; }
    public string? AwsSecretKey { get; set; }
    public string? AwsSessionToken { get; set; }
}
