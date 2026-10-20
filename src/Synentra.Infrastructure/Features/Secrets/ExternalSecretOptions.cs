namespace Synentra.Infrastructure.Features.Secrets;

public sealed class ExternalSecretOptions
{
    public string? VaultUrl { get; set; }
    public string VaultMount { get; set; } = "secret";
    public string VaultToken { get; set; } = "";
    public string? AzureVaultUrl { get; set; }
    public string? AwsRegion { get; set; }
}
