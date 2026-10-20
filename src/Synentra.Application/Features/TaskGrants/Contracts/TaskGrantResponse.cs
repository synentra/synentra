namespace Synentra.Application.Features.TaskGrants.Contracts;

public sealed record TaskGrantResponse(
    string Id, 
    string State, 
    DateTimeOffset ExpiresAt, 
    IReadOnlyList<GrantLimitStateDto> LimitStates);
