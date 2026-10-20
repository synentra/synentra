using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Invocations;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Normalization;

namespace Synentra.Application.Features.Invocations.Commands.NormalizeInvocation;

public sealed record NormalizeInvocationCommand(
    IntentModality Modality,
    NormalizationInput Input, 
    CanonicalPrincipal Principal) : ICommand<ToolInvocation>;
