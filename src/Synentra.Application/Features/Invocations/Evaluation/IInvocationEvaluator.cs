using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Invocations;

namespace Synentra.Application.Features.Invocations.Evaluation;

public interface IInvocationEvaluator
{
    Task<ExecutionDecision> EvaluateAsync(ToolInvocation invocation, CancellationToken ct = default);
}
