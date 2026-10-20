using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Admission;

namespace Synentra.Application.Features.Invocations.Commands.AdmitInvocation;

public sealed class AdmitInvocationCommandHandler : IActionHandler<AdmitInvocationCommand, AdmissionResult>
{
    private readonly IExecutionAdmission _service;

    public AdmitInvocationCommandHandler(IExecutionAdmission service)
    {
        _service = service;
    }

    public async Task<AdmissionResult> Handle(AdmitInvocationCommand request, CancellationToken cancellationToken = default)
    {
        var decision = request.Decision;
        var invocation = request.Invocation;
        var ct = cancellationToken;
        return await _service.AdmitAsync(decision, invocation, ct);
    }
}
