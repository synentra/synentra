using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Operations.Contracts;

namespace Synentra.Application.Features.Operations.Queries.ListOperations;

public sealed record ListOperationsQuery(
    string AgentId, 
    int PageNumber = 1, 
    int PageSize = 50) : IQuery<IReadOnlyList<OperationDto>>;
