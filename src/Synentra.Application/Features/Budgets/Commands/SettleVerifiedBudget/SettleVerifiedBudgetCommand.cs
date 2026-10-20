using Synentra.Domain.Invocations.ValueObjects;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Budgets.Contracts;
using Synentra.Application.Features.Budgets;
using Synentra.Application.Features.Executions.Contracts;
using Unit = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Features.Budgets.Commands.SettleVerifiedBudget;
public sealed record SettleVerifiedBudgetCommand(string ExecutionId, string InvocationId, IReadOnlyList<(string Measure, string Unit, int Scale, string Scope, string? BudgetIdentity, long SettledAmount)> Amounts) : ICommand<Unit>;
