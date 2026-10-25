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
    public bool Revoked { get; }
    public string AuthorityVersion { get; }

    public bool HasSameAuthority(CredentialBinding other) 
        => !Revoked 
        && !other.Revoked 
        && Id == other.Id 
        && AuthorityVersion == other.AuthorityVersion 
        && TargetRef == other.TargetRef 
        && AllowedPrincipal == other.AllowedPrincipal 
        && DelegationScope == other.DelegationScope 
        && AllowedOperations.SetEquals(other.AllowedOperations);

    public CredentialBinding(
        string id, 
        string targetRef, 
        string allowedPrincipal, 
        string delegationScope, 
        IReadOnlySet<string> allowedOperations, 
        SecretProviderRef secretRef, 
        string version, 
        bool revoked = false,
        string? authorityVersion = null)
    {
        Id = id;
        TargetRef = targetRef;
        AllowedPrincipal = allowedPrincipal;
        DelegationScope = delegationScope;
        AllowedOperations = new HashSet<string>(allowedOperations, StringComparer.Ordinal);
        SecretRef = secretRef;
        Version = version;
        Revoked = revoked;
        AuthorityVersion = authorityVersion ?? version;
    }
}
