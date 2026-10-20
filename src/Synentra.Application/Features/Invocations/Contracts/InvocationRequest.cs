using System.Text.Json;

namespace Synentra.Application.Features.Invocations.Contracts;

public sealed record InvocationRequest
{
    public string SchemaVersion { get; init; } = "synentra.invocation.v1";
    public required string OperationId { get; init; }
    public string? BindingId { get; init; }
    public string? TaskGrantId { get; init; }
    public required JsonElement Arguments { get; init; }
    public InvocationContext? Context { get; init; }
}
