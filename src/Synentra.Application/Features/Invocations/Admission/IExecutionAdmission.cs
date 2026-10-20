using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Invocations;

namespace Synentra.Application.Features.Invocations.Admission;

public interface IExecutionAdmission
{
    Task<AdmissionResult> AdmitAsync(
        ExecutionDecision decision, 
        ToolInvocation invocation, 
        CancellationToken ct = default);
}
