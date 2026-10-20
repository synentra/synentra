namespace Synentra.Domain.Security.ValueObjects;

public sealed record CanonicalPrincipal
{
    public string AgentId { get; }
    public string SecurityScope { get; }
    public string Issuer { get; }
    public string Subject { get; }
    public IReadOnlySet<string> Scopes { get; }
    public IReadOnlyDictionary<string, string> Claims { get; }
    public PrincipalType Type { get; }

    public CanonicalPrincipal(
        string agentId, 
        string securityScope, 
        string issuer, 
        string subject, 
        IReadOnlySet<string> scopes, 
        IReadOnlyDictionary<string, string> claims, 
        PrincipalType type)
    {
        AgentId = agentId;
        SecurityScope = securityScope;
        Issuer = issuer;
        Subject = subject;
        Scopes = scopes;
        Claims = claims;
        Type = type;
    }
}
