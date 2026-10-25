namespace Synentra.Infrastructure.Configuration.Secrets;

public class AzureKeyVaultConfiguration
{
    public string? AzureVaultUrl { get; set; }
    public string AzureIdentity { get; set; } = "workload";
    public string? AzureClientId { get; set; }
    public string? AzureTenantId { get; set; }
    public string? AzureClientSecret { get; set; }
}
