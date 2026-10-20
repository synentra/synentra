using Synentra.Domain.Invocations;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Contracts;

namespace Synentra.Application.Features.Invocations.Queries.GetInvocation;

public sealed class GetInvocationQueryHandler : IActionHandler<GetInvocationQuery, ToolInvocation?>
{
    private readonly IInvocationStore _store;
    public GetInvocationQueryHandler(IInvocationStore store)
    {
        _store = store;
    }

    public async Task<ToolInvocation?> Handle(GetInvocationQuery request, CancellationToken cancellationToken = default)
    {
        var invocationId = request.InvocationId;
        var ct = cancellationToken;
        return await _store.GetAsync(invocationId, ct);
    }
}
