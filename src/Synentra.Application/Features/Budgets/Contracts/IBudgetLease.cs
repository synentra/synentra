namespace Synentra.Application.Features.Budgets.Contracts;
public interface IBudgetLease : IAsyncDisposable
{
    long AuthorizedLimit { get; }

    long Settled { get; }

    long ActiveReserved { get; }

    long Available => AuthorizedLimit - Settled - ActiveReserved;

    bool CanReserve(long amount);
    Task Reserve(long amount, string reservationId);
    void Settle(long amount, string reservationId);
    void Release(long amount, string reservationId, string evidenceRef);
}
