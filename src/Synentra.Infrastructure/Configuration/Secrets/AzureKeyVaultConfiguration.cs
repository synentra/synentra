namespace Synentra.Infrastructure.Configuration.Secrets;

public class AzureKeyVaultConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string Identity { get; set; } = "workload";
    public string? ClientId { get; set; }
    public string? TenantId { get; set; }
    public string? ClientSecret { get; set; }
}
