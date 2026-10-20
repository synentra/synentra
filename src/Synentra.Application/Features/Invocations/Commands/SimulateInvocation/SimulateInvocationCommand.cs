using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Features.Invocations.Commands.SimulateInvocation;

public sealed record SimulateInvocationCommand(
    InvocationRequest Request, 
    CanonicalPrincipal Principal, 
    IntentModality Modality = IntentModality.Http) : ICommand<InvocationResultDto>;
