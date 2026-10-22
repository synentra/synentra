using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Operations.Contracts;
using Synentra.Application.Features.Operations.Queries.ListOperations;
using Synentra.Domain.Common.Results;

namespace Synentra.Application.Features.Operations;

public static class OperationDispatcherExtensions
{
    public static async Task<PaginatedResult<OperationDto>> ListOperations(
        this IDispatcher dispatcher,
        string agentId,
        int? pageNumber, 
        int? pageSize,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new ListOperationsQuery(agentId, new PageRequest(pageNumber ?? 1, pageSize ?? 20)), cancellationToken);
    }
}
