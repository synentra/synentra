using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Operations.Contracts;
using Synentra.Domain.Common.Results;

namespace Synentra.Application.Features.Operations.Queries.ListOperations;

public sealed record ListOperationsQuery(
    string AgentId, 
    PageRequest PageRequest) : IQuery<PaginatedResult<OperationDto>>;
