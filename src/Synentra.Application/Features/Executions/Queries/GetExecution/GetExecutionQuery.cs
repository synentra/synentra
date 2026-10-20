using Synentra.Domain.Execution;
using Synentra.Application.Common.Dispatching;

namespace Synentra.Application.Features.Executions.Queries.GetExecution;

public sealed record GetExecutionQuery(string ExecutionId) : IQuery<ExecutionRecord?>;
