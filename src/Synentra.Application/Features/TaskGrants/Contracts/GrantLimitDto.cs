namespace Synentra.Application.Features.TaskGrants.Contracts;

public sealed record GrantLimitDto(
    string Measure, 
    string Unit, 
    int Scale, 
    long? PerInvocation, 
    long? Aggregate);
