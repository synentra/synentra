namespace Synentra.Application.Features.TaskGrants.Contracts;

public sealed record ResourceScopeDto(
    string Role, 
    string Type, 
    IReadOnlyList<string> Ids);
