using Synentra.Domain.Governance.Decisions;

namespace Synentra.Application.Features.Invocations.Admission;

public sealed record AdmissionResult(
    bool Admitted, 
    ExecutionDecision Decision, 
    string? ClaimId = null)
{
    public static AdmissionResult CreateAdmitted(
        string claimId, 
        ExecutionDecision decision) => new(true, decision, claimId);

    public static AdmissionResult NotAdmitted(ExecutionDecision decision) => new(false, decision);
}
