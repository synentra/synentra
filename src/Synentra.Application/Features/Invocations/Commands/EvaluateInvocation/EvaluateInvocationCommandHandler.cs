using Synentra.Domain.Governance.Decisions;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Evaluation;

namespace Synentra.Application.Features.Invocations.Commands.EvaluateInvocation;

public sealed class EvaluateInvocationCommandHandler : IActionHandler<EvaluateInvocationCommand, ExecutionDecision>
{
    private readonly IInvocationEvaluator _service;

    public EvaluateInvocationCommandHandler(IInvocationEvaluator service)
    {
        _service = service;
    }

    public async Task<ExecutionDecision> Handle(EvaluateInvocationCommand request, CancellationToken cancellationToken = default)
    {
        var invocation = request.Invocation;
        var ct = cancellationToken;
        return await _service.EvaluateAsync(invocation, ct);
    }
}
