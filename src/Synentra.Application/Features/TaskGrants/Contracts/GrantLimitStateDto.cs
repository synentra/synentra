namespace Synentra.Application.Features.TaskGrants.Contracts;

public sealed record GrantLimitStateDto(
    string Measure, 
    string Unit, 
    long AuthorizedAggregate, 
    long Settled, 
    long ActiveReserved, 
    long Available);
