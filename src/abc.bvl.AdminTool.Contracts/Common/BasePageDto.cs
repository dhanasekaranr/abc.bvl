namespace abc.bvl.AdminTool.Contracts.Common;

/// <summary>
/// Base DTO that all data transfer objects must inherit from
/// Contains metadata needed by UI for access control, correlation tracking, and pagination
/// </summary>
public abstract record BasePageDto
{
    /// <summary>
    /// Correlation ID for request tracking across distributed systems
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Server timestamp when response was generated
    /// </summary>
    public DateTimeOffset ServerTime { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User information from authentication context
    /// </summary>
    public UserInfo? User { get; init; }

    /// <summary>
    /// Access control information for UI to enable/disable controls
    /// </summary>
    public AccessInfo? Access { get; init; }

    /// <summary>
    /// Optional pagination information for list responses
    /// Null for non-paginated responses
    /// </summary>
    public PaginationInfo? Pagination { get; init; }
}

/// <summary>
/// Pagination metadata for list responses
/// </summary>
public record PaginationInfo
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public long TotalItems { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// True if there is a previous page
    /// </summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>
    /// True if there is a next page
    /// </summary>
    public bool HasNext => CurrentPage < TotalPages;

    /// <summary>
    /// Index of first item on current page
    /// </summary>
    public long FirstItemIndex => ((CurrentPage - 1) * PageSize) + 1;

    /// <summary>
    /// Index of last item on current page
    /// </summary>
    public long LastItemIndex => Math.Min(FirstItemIndex + PageSize - 1, TotalItems);

    public PaginationInfo(int currentPage, int pageSize, long totalItems)
    {
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 0;
    }
}

/// <summary>
/// Generic paged result wrapper
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public record PagedResult<T> : BasePageDto where T : class
{
    /// <summary>
    /// The actual data items for current page
    /// </summary>
    public IEnumerable<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// Total count of items (required for PagedResult)
    /// </summary>
    public long TotalCount => Pagination?.TotalItems ?? 0;

    public PagedResult()
    {
    }

    public PagedResult(
        IEnumerable<T> items, 
        int currentPage, 
        int pageSize, 
        long totalItems)
    {
        Items = items;
        Pagination = new PaginationInfo(currentPage, pageSize, totalItems);
        // User, Access, and CorrelationId will be auto-populated by EnrichResponseFilter
    }
}

/// <summary>
/// Generic single result wrapper
/// </summary>
/// <typeparam name="T">Type of data</typeparam>
public record SingleResult<T> : BasePageDto where T : class
{
    /// <summary>
    /// The actual data
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Optional message for additional context
    /// </summary>
    public string? Message { get; init; }

    public SingleResult()
    {
    }

    public SingleResult(
        T? data,
        bool success = true,
        string? message = null)
    {
        Data = data;
        Success = success;
        Message = message;
        // User, Access, and CorrelationId will be auto-populated by EnrichResponseFilter
    }
}