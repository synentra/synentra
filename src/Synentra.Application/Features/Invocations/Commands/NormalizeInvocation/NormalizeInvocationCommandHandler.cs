using Synentra.Domain.Invocations;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Normalization;

namespace Synentra.Application.Features.Invocations.Commands.NormalizeInvocation;

public sealed class NormalizeInvocationCommandHandler : IActionHandler<NormalizeInvocationCommand, ToolInvocation>
{
    private readonly InvocationNormalizer _service;

    public NormalizeInvocationCommandHandler(InvocationNormalizer service)
    {
        _service = service;
    }

    public async Task<ToolInvocation> Handle(
        NormalizeInvocationCommand request, 
        CancellationToken cancellationToken = default)
    {
        var modality = request.Modality;
        var input = request.Input;
        var principal = request.Principal;
        var ct = cancellationToken;
        return await _service.NormalizeAsync(modality, input, principal, ct);
    }
}
