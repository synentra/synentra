using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Budgets.Commands.ReleaseUnusedBudget;
using Synentra.Application.Features.Budgets.Commands.SettleVerifiedBudget;
using Synentra.Application.Features.Budgets.Contracts;
using Synentra.Application.Features.Budgets;
using Synentra.Application.Features.Executions.Contracts;

namespace Synentra.Application.Features.Budgets;
/// <summary>Compatibility facade. New callers should dispatch the feature request directly.</summary>
public sealed class BudgetSettlementService
{
    private readonly IDispatcher _dispatcher;
    public BudgetSettlementService(IDispatcher dispatcher) => _dispatcher = dispatcher;
    public Task SettleVerifiedAsync(string executionId, string invocationId, IReadOnlyList<(string Measure, string Unit, int Scale, string Scope, string? BudgetIdentity, long SettledAmount)> amounts, CancellationToken ct) => _dispatcher.Dispatch(new SettleVerifiedBudgetCommand(executionId, invocationId, amounts), ct);
    public Task ReleaseUnusedAsync(string executionId, string invocationId, string evidenceRef, IReadOnlyList<(string Measure, string Unit, int Scale, string Scope, string? BudgetIdentity, long Amount)> amounts, CancellationToken ct) => _dispatcher.Dispatch(new ReleaseUnusedBudgetCommand(executionId, invocationId, evidenceRef, amounts), ct);
}
