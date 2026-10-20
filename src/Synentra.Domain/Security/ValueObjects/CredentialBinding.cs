namespace Synentra.Domain.Security.ValueObjects;

public sealed record CredentialBinding
{
    public string Id { get; }
    public string TargetRef { get; }
    public string AllowedPrincipal { get; }
    public string DelegationScope { get; }
    public IReadOnlySet<string> AllowedOperations { get; }
    public SecretProviderRef SecretRef { get; }
    public string Version { get; }

    public CredentialBinding(
        string id, 
        string targetRef, 
        string allowedPrincipal, 
        string delegationScope, 
        IReadOnlySet<string> allowedOperations, 
        SecretProviderRef secretRef, 
        string version)
    {
        Id = id;
        TargetRef = targetRef;
        AllowedPrincipal = allowedPrincipal;
        DelegationScope = delegationScope;
        AllowedOperations = allowedOperations;
        SecretRef = secretRef;
        Version = version;
    }
}
