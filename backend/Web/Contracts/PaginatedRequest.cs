namespace Web.Contracts;

// Bound from the query string: ?pageNumber=2&pageSize=50
public record PaginatedRequest(int PageNumber = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    // Normalizing
    public int Page => PageNumber < 1 ? 1 : PageNumber;
    public int Size => PageSize < 1 ? DefaultPageSize : Math.Min(PageSize, MaxPageSize);
}
