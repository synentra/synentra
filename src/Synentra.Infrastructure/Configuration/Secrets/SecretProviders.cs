using Synentra.Infrastructure.Configuration.SecretManagement;

namespace Synentra.Infrastructure.Configuration.Secrets;

public class SecretProviders
{
    public EncryptedDbConfiguration EncryptedDatabase { get; set; } = new();
    public AwsSecretsConfiguration AwsSecrets { get; set; } = new();
    public AzureKeyVaultConfiguration AzureKeyVault { get; set; } = new();
    public HashiCorpVaultConfiguration HashiCorpVault { get; set; } = new();
}