using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Domain.Invocations.ValueObjects;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Budgets.Contracts;
using Synentra.Application.Features.Budgets;
using Synentra.Application.Features.Executions.Contracts;
using Unit = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Features.Budgets.Commands.SettleVerifiedBudget;
public sealed class SettleVerifiedBudgetCommandHandler : IActionHandler<SettleVerifiedBudgetCommand, Unit>
{
    private readonly IBudgetStore _budgets;
    private readonly IInvocationStore _invocations;
    private readonly IGrantStore _grants;
    private readonly IAuditStore _audit;
    public SettleVerifiedBudgetCommandHandler(IBudgetStore budgets, IAuditStore audit, IInvocationStore invocations, IGrantStore grants)
    {
        _budgets = budgets;
        _invocations = invocations;
        _grants = grants;
        _audit = audit;
    }

    public async Task<Unit> Handle(SettleVerifiedBudgetCommand request, CancellationToken cancellationToken = default)
    {
        var executionId = request.ExecutionId;
        var invocationId = request.InvocationId;
        var amounts = request.Amounts;
        var ct = cancellationToken;
        foreach (var a in amounts)
        {
            var key = new BudgetKey(a.Measure, a.Unit, a.Scale, a.Scope, a.BudgetIdentity);
            await using var lease = await _budgets.LockAsync(key, ct);
            lease.Settle(a.SettledAmount, executionId);
        }

        await _audit.AppendAsync(new AuditEntry(Guid.NewGuid().ToString("N"), "budget-settled", "system", "default", null, null, null, invocationId, null, null, null, new Dictionary<string, string> { ["executionId"] = executionId }, DateTimeOffset.UtcNow), ct);
        var invocation = await _invocations.GetAsync(invocationId, ct) ?? throw new InvalidOperationException("Invocation missing.");
        if (invocation.TaskGrantId is { } grantId)
        {
            var grant = await _grants.GetAsync(grantId, ct) ?? throw new InvalidOperationException("Grant missing.");
            foreach (var group in amounts.GroupBy(a => (a.Measure, a.Unit, a.Scale)))
            {
                var limit = grant.Limits.SingleOrDefault(l => l.Measure == group.Key.Measure && l.Unit == group.Key.Unit && l.Scale == group.Key.Scale);
                if (limit?.Aggregate is null)
                    continue;
                await using var lease = await _budgets.LockGrantAsync(grantId, group.Key.Measure, group.Key.Unit, group.Key.Scale, null, ct);
                lease.Settle(group.Aggregate(0L, (sum, a) => checked(sum + a.SettledAmount)), executionId);
            }
        }

        return Unit.Value;
    }
}
