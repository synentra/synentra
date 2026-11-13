using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;
using Synentra.Domain.Grants.ValueObjects;

namespace Synentra.Domain.Grants;

public sealed class TaskGrant
{
    public string Id { get; }
    public string IssuerId { get; }
    public string AgentId { get; }
    public string SecurityScope { get; }
    public IReadOnlyList<string> OperationIds { get; }
    public IReadOnlyList<ResourceScope> ResourceScopes { get; }
    public DateTimeOffset ExpiresAt { get; }
    public string? ExternalReference { get; }
    public GrantState State { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public TaskGrant(
        string id, 
        string issuerId,
        string agentId, 
        string securityScope,
        IReadOnlyList<string> operationIds,
        IReadOnlyList<ResourceScope> resourceScopes, 
        DateTimeOffset expiresAt, 
        string? externalReference)
    {
        Id = id;
        IssuerId = issuerId;
        AgentId = agentId;
        SecurityScope = securityScope;
        OperationIds = operationIds;
        ResourceScopes = resourceScopes;
        ExpiresAt = expiresAt;
        ExternalReference = externalReference;
        State = GrantState.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke()
    {
        if (State != GrantState.Active)
            throw new DomainException(DomainErrors.TaskGrant.CannotRevoke(State.ToString()));
        State = GrantState.Revoked;
    }

    public bool IsValidAt(DateTimeOffset t) => State == GrantState.Active && t < ExpiresAt;

    public static TaskGrant Restore(
        string id,
        string issuerId,
        string agentId,
        string scope,
        IReadOnlyList<string> operations,
        IReadOnlyList<ResourceScope> resources,
        DateTimeOffset expiresAt,
        string? reference,
        GrantState state,
        DateTimeOffset createdAt) 
        => new(id, issuerId, agentId, scope, operations, resources, expiresAt, reference)
        {
            State = state,
            CreatedAt = createdAt
        };
}
