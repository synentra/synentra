using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Domain.Governance.Trust.ValueObjects;
using Synentra.Application.Features.Invocations.Contracts;

namespace Synentra.Application.Features.Risk.Contracts;

public sealed record RiskInput(
    ToolInvocationData Invocation, 
    IntentLabel? Intent, 
    TrustSnapshot Trust);
