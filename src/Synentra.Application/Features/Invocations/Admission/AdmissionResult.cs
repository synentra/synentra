using Synentra.Domain.Effects.ValueObjects;
using Synentra.Domain.Governance.Decisions;

namespace Synentra.Application.Features.Invocations.Admission;

public sealed record AdmissionResult(
    bool Admitted, 
    ExecutionDecision Decision, 
    string? ClaimId = null, 
    EffectPlan? EffectPlan = null)
{
    public static AdmissionResult CreateAdmitted(
        string claimId, 
        EffectPlan plan, 
        ExecutionDecision decision) => new(true, decision, claimId, plan);

    public static AdmissionResult NotAdmitted(ExecutionDecision decision) => new(false, decision);
}
