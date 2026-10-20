namespace Synentra.Application.Features.Budgets.Contracts;
public sealed record BudgetKey(string Measure, string Unit, int Scale, string Scope, string? BudgetIdentity);
