namespace Synentra.Infrastructure.Configuration.Secrets;

public class AwsSecretsConfiguration
{
    public string? Region { get; set; }
    public string Identity { get; set; } = "web-identity";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? SessionToken { get; set; }
}
