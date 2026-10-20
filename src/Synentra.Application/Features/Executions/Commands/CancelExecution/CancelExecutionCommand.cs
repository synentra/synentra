using Synentra.Application.Common.Dispatching;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Features.Executions.Commands.CancelExecution;

public sealed record CancelExecutionCommand(string ExecutionId, CanonicalPrincipal Caller) : ICommand<bool>;
