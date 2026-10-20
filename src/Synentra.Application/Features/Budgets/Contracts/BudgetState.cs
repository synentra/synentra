namespace Synentra.Application.Features.Budgets.Contracts;
public sealed record BudgetState(long AuthorizedLimit, long Settled, long ActiveReserved)
{
    public long Available => AuthorizedLimit - Settled - ActiveReserved;
}
