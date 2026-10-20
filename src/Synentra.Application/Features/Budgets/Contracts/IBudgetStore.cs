namespace Synentra.Application.Features.Budgets.Contracts;
public interface IBudgetStore
{
    Task<IBudgetLease> LockGrantAsync(string grantId, string measure, string unit, int scale, long? authorizedLimit, CancellationToken ct);
    Task<IBudgetLease> LockAsync(BudgetKey key, CancellationToken ct);
    Task<BudgetState?> GetGrantStateAsync(string grantId, string measure, string unit, int scale, CancellationToken ct);
    Task ReleaseAllForExecutionAsync(string executionId, string evidenceRef, CancellationToken ct);
}
