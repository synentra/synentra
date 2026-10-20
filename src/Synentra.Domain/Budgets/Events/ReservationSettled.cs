namespace Synentra.Domain.Budgets.Events;

public sealed record ReservationSettled(
    string ReservationId, 
    long SettledAmount, 
    DateTimeOffset OccurredAt);
