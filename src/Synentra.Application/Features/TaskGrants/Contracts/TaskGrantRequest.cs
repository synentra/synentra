namespace Synentra.Application.Features.TaskGrants.Contracts;

public sealed record TaskGrantRequest(
    string AgentId, 
    IReadOnlyList<string> OperationIds, 
    IReadOnlyList<ResourceScopeDto> ResourceScopes, 
    DateTimeOffset ExpiresAt, 
    string? ExternalReference);
