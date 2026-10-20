namespace Synentra.Domain.Grants.ValueObjects;

public sealed record ResourceScope(
    string Role, 
    string Type, 
    IReadOnlyList<string> Ids);
