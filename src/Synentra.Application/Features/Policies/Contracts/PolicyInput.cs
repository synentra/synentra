using Synentra.Domain.Effects.ValueObjects;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Domain.Governance.Risk.ValueObjects;
using Synentra.Domain.Governance.Trust.ValueObjects;
using Synentra.Domain.Resources.ValueObjects;
using Synentra.Application.Features.Invocations.Contracts;

namespace Synentra.Application.Features.Policies.Contracts;

public sealed record PolicyInput(
    ToolInvocationData Invocation, 
    string AuthenticatedAgentId, 
    string SecurityScope, 
    IntentLabel? Intent, 
    RiskAssessment? Risk, 
    TrustSnapshot? Trust, 
    IReadOnlyList<ResourceDescriptor> Resources, 
    EffectPlan? EffectPlan, 
    string? TaskGrantId, 
    IReadOnlyDictionary<string, string> UntrustedContext);
