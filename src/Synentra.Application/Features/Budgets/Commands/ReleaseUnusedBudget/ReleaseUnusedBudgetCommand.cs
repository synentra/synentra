using Synentra.Application.Common.Dispatching;
using Unit = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Features.Budgets.Commands.ReleaseUnusedBudget;

public sealed record ReleaseUnusedBudgetCommand(
    string ExecutionId, 
    string InvocationId, 
    string EvidenceRef, 
    IReadOnlyList<(string Measure, string Unit, int Scale, string Scope, string? BudgetIdentity, long Amount)> Amounts) : ICommand<Unit>;
