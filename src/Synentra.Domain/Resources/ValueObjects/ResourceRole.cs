namespace Synentra.Domain.Resources.ValueObjects;

public sealed record ResourceRole(
    string Role,
    IReadOnlyList<ResourceDescriptor> Descriptors)
{
    public bool IsEmpty => Descriptors.Count == 0;
}
