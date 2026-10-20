using Synentra.Domain.Execution;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Executions.Contracts;

namespace Synentra.Application.Features.Executions.Queries.GetExecution;

public sealed class GetExecutionQueryHandler : IActionHandler<GetExecutionQuery, ExecutionRecord?>
{
    private readonly IExecutionRecordStore _store;
    public GetExecutionQueryHandler(IExecutionRecordStore store)
    {
        _store = store;
    }

    public async Task<ExecutionRecord?> Handle(GetExecutionQuery request, CancellationToken cancellationToken = default)
    {
        var executionId = request.ExecutionId;
        var ct = cancellationToken;
        return await _store.GetAsync(executionId, ct);
    }
}
