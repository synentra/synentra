using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Invocations;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Admission;

namespace Synentra.Application.Features.Invocations.Commands.AdmitInvocation;

public sealed record AdmitInvocationCommand(
    ExecutionDecision Decision, 
    ToolInvocation Invocation) : ICommand<AdmissionResult>;
