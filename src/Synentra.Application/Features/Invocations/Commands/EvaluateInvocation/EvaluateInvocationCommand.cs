using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Invocations;
using Synentra.Application.Common.Dispatching;

namespace Synentra.Application.Features.Invocations.Commands.EvaluateInvocation;

public sealed record EvaluateInvocationCommand(ToolInvocation Invocation) : ICommand<ExecutionDecision>;
