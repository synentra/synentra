namespace Synentra.Infrastructure.Configuration.SecretManagement;

public enum SecretManagementProviderType
{
    None,
    EnvironmentVariables,
    AzureKeyVault,
    UserSecrets,
}