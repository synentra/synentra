using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Domain.Resources.ValueObjects;

public sealed record ResourceDescriptor
{
    public string Type { get; }
    public string Id { get; }
    public string SecurityScope { get; }
    public string AuthorityDomain { get; }
    public string? Version { get; }
    public IReadOnlyDictionary<string, string> TrustedAttributes { get; }
    public DateTimeOffset ObservedAt { get; }

    public ResourceDescriptor(
        string type, string id, 
        string securityScope, string authorityDomain, 
        string? version = null, 
        IReadOnlyDictionary<string, string>? trustedAttributes = null, 
        DateTimeOffset? observedAt = null)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException(DomainErrors.ResourceDescriptor.InvalidResourceType);

        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException(DomainErrors.ResourceDescriptor.InvalidResourceId);

        if (string.IsNullOrWhiteSpace(securityScope))
            throw new DomainException(DomainErrors.ResourceDescriptor.InvalidSecurityScope);

        if (string.IsNullOrWhiteSpace(authorityDomain))
            throw new DomainException(DomainErrors.ResourceDescriptor.InvalidAuthorityDomain);

        Type = type;
        Id = id;
        SecurityScope = securityScope;
        AuthorityDomain = authorityDomain;
        Version = version;
        TrustedAttributes = trustedAttributes ?? new Dictionary<string, string>();
        ObservedAt = observedAt ?? DateTimeOffset.UtcNow;
    }

    public string CanonicalKey => $"{SecurityScope}|{AuthorityDomain}|{Type}|{Id}";

    public bool SameIdentityAs(ResourceDescriptor other) => string.Equals(CanonicalKey, other.CanonicalKey, StringComparison.Ordinal);
}
