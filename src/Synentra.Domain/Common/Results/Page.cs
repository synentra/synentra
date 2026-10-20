namespace Synentra.Domain.Common.Results;

public sealed record Page<T>(IReadOnlyList<T> Items, int TotalCount);
