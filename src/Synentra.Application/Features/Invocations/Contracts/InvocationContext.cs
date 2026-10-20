namespace Synentra.Application.Features.Invocations.Contracts;

public sealed record InvocationContext
{
    public string? WorkflowId { get; init; } // untrusted evidence
    public string? DeclaredPurpose { get; init; } // untrusted evidence
}
