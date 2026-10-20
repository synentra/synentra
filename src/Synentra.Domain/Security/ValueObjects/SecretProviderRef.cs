namespace Synentra.Domain.Security.ValueObjects;

public sealed record SecretProviderRef(
    string ProviderId, 
    string SecretPath, 
    string Version);
