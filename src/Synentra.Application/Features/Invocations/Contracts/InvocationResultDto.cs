namespace Synentra.Application.Features.Invocations.Contracts;

public sealed record InvocationResultDto
{
    public string SchemaVersion { get; init; } = "synentra.invocation-result.v1";
    public required string InvocationId { get; init; }
    public required DecisionDto Decision { get; init; }
    public ApprovalDto? Approval { get; init; }
    public required ExecutionDto Execution { get; init; }
    public required OutcomeDto Outcome { get; init; }
}
