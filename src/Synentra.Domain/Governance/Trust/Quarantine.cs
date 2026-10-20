using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Domain.Governance.Trust;

public sealed class Quarantine
{
    public string Id { get; }
    public string AgentId { get; }
    public string SecurityScope { get; }
    public string Reason { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public bool Active => ReleasedAt is null;

    public Quarantine(
        string id, 
        string agentId, 
        string securityScope, 
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(securityScope);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Id = id;
        AgentId = agentId;
        SecurityScope = securityScope;
        Reason = reason;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Release(string releasedBy)
    {
        if (!Active)
            throw new DomainException(DomainErrors.Quarantine.AlreadyReleased);
        ReleasedAt = DateTimeOffset.UtcNow;
    }
}
