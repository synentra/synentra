namespace Synentra.Application.Features.Invocations.Contracts;

public sealed record ExecutionDto(
    string Status,
    string? UpstreamReference = null,
    int? ResponseStatusCode = null,
    string? ResponsePayload = null,
    string? ErrorMessage = null,
    DateTimeOffset? DispatchedAt = null,
    DateTimeOffset? RespondedAt = null);