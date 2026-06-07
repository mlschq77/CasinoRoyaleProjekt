using Microsoft.EntityFrameworkCore;

namespace CasinoRoyale.Services;

public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public PaginatedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
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
