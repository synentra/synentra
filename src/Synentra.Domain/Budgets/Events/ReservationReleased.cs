namespace Synentra.Domain.Budgets.Events;

public sealed record ReservationReleased(
    string ReservationId, 
    long ReleasedAmount, 
    string EvidenceRef, 
    DateTimeOffset OccurredAt);
