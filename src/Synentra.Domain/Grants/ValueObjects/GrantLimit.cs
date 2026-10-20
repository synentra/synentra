namespace Synentra.Domain.Grants.ValueObjects;

public sealed record GrantLimit(
    string Measure, 
    string Unit, 
    int Scale, 
    long? PerInvocation, 
    long? Aggregate);
