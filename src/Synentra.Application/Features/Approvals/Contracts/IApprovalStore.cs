using Synentra.Domain.Approvals;
using Synentra.Domain.Common.Results;

namespace Synentra.Application.Features.Approvals.Contracts;

public interface IApprovalStore
{
    Task<Approval?> GetAsync(string approvalId, CancellationToken ct);
    Task<Approval?> FindByInvocationAsync(string invocationId, CancellationToken ct);
    Task SaveAsync(Approval approval, CancellationToken ct);
    Task<Page<Approval>> ListPendingAsync(string securityScope, PageRequest pageRequest, CancellationToken ct);
}
