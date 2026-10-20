namespace Synentra.Domain.Common.Results;

public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public int PageNumber { get; }
    public int PageSize { get; }

    public PageRequest(int? pageNumber = 1, int? pageSize = DefaultPageSize)
    {
        PageNumber = Math.Max(1, pageNumber ?? 1);
        PageSize = Math.Clamp(
            pageSize <= 0 ? DefaultPageSize : pageSize ?? DefaultPageSize,
            1,
            MaxPageSize);
    }
}