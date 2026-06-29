namespace PhysioAssist.Api.Shared.Pagination;

public class PaginationList<T>(List<T> items, int pageNumber, int count, int pageSize)
    where T : notnull
{
    public IEnumerable<T> Items { get; private set; } = items;
    public int PageNumber { get; private set; } = pageNumber;
    public int PageSize { get; private set; } = pageSize;
    public int TotalPages => (int)Math.Ceiling(count / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static async Task<PaginationList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = await source.CountAsync();
        var Items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PaginationList<T>(Items, pageNumber, count, pageSize);
    }

}

