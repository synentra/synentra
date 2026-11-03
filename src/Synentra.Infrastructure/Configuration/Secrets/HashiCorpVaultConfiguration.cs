namespace Synentra.Infrastructure.Configuration.SecretManagement;

public class HashiCorpVaultConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string Mount { get; set; } = "secret";
    public string Token { get; set; } = "";
}