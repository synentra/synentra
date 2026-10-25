namespace Synentra.Infrastructure.Configuration.SecretManagement;

public class HashiCorpVaultConfiguration
{
    public string? VaultUrl { get; set; }
    public string VaultMount { get; set; } = "secret";
    public string VaultToken { get; set; } = "";
}