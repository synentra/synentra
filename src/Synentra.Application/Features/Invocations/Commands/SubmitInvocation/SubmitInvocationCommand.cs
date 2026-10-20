using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Features.Invocations.Commands.SubmitInvocation;

public sealed record SubmitInvocationCommand(
    InvocationRequest Request, 
    CanonicalPrincipal Principal, 
    string? IdempotencyKey, 
    IntentModality Modality = IntentModality.Http) : ICommand<InvocationResultDto>;
