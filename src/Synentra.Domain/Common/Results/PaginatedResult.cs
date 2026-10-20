using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Domain.Common.Results;

public class PaginatedResult<T> : Result
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    private PaginatedResult(IReadOnlyList<T> items, PageRequest pageRequest, int totalCount)
        : base(true, null)
    {
        Items = items;
        PageNumber = pageRequest.PageNumber;
        PageSize = pageRequest.PageSize;
        TotalCount = totalCount;
    }

    private PaginatedResult(Diagnostic diagnostic)
        : base(false, diagnostic)
    {
        Items = Array.Empty<T>();
    }

    public static PaginatedResult<T> Success(IReadOnlyList<T> items, PageRequest pageRequest, int totalCount)
        => new(items, pageRequest, totalCount);

    public static Task<PaginatedResult<T>> SuccessAsync(IReadOnlyList<T> items, PageRequest pageRequest, int totalCount)
        => Task.FromResult(Success(items, pageRequest, totalCount));

    public static new PaginatedResult<T> Failure(Diagnostic diagnostic) => new(diagnostic);

    public static new Task<PaginatedResult<T>> FailureAsync(Diagnostic diagnostic)
        => Task.FromResult(Failure(diagnostic));
}