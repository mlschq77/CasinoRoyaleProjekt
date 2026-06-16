using Microsoft.EntityFrameworkCore;

namespace CasinoRoyale.Services;

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    public PaginatedResult() { }

    public PaginatedResult(List<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        HasPrevious = page > 1;
        HasNext = page < TotalPages;
    }
}

public static class PaginationHelper
{
    public static async Task<PaginatedResult<T>> PaginateAsync<T>(
        IQueryable<T> query,
        int page,
        int? pageSize = null,
        int maxPageSize = 100,
        int defaultPageSize = 20,
        CancellationToken ct = default)
    {
        var size = Math.Clamp(pageSize ?? defaultPageSize, 1, maxPageSize);
        var count = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PaginatedResult<T>(items, count, page, size);
    }

    public static async Task<PaginatedResult<T>> PaginateKeysetAsync<T, TKey>(
        IQueryable<T> query,
        TKey? lastSeenKey,
        int? pageSize = null,
        int maxPageSize = 100,
        int defaultPageSize = 20,
        CancellationToken ct = default)
        where TKey : struct
    {
        var size = Math.Clamp(pageSize ?? defaultPageSize, 1, maxPageSize);
        var items = await query
            .Take(size)
            .ToListAsync(ct);

        return new PaginatedResult<T>(items, items.Count, 1, size);
    }
}
