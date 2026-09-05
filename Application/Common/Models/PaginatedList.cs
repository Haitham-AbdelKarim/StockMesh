namespace Application.Common.Models;

public sealed class PaginatedList<T>
{
    public PaginatedList(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page < 1 ? 1 : page;
        PageSize = pageSize < 1 ? 1 : pageSize;
        TotalPages = TotalCount == 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages { get; }

    public bool HasPreviousPage
    {
        get { return Page > 1; }
    }

    public bool HasNextPage
    {
        get { return Page < TotalPages; }
    }

    public static PaginatedList<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int page,
        int pageSize)
    {
        return new PaginatedList<T>(items, totalCount, page, pageSize);
    }
}