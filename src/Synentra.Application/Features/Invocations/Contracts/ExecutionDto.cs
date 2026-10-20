namespace Synentra.Application.Features.Invocations.Contracts;

public sealed record ExecutionDto(string Status, string? UpstreamReference = null);
