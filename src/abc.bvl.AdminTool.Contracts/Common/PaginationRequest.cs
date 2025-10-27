using System.ComponentModel.DataAnnotations;

namespace abc.bvl.AdminTool.Contracts.Common;

/// <summary>
/// Base class for pagination request parameters
/// </summary>
public record PaginationRequest
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// Page number (1-based). Minimum: 1
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1")]
    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page. Range: 1-100, Default: 20
    /// </summary>
    [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }

    /// <summary>
    /// Calculate skip count for database query
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Get take count for database query (alias for PageSize)
    /// </summary>
    public int Take => PageSize;

    /// <summary>
    /// Optional sort field
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Sort direction: asc or desc
    /// </summary>
    public string SortDirection { get; init; } = "asc";

    /// <summary>
    /// Check if sorting is ascending
    /// </summary>
    public bool IsAscending => SortDirection?.ToLowerInvariant() == "asc";

    /// <summary>
    /// Optional search/filter term
    /// </summary>
    [MaxLength(100, ErrorMessage = "Search term cannot exceed 100 characters")]
    public string? SearchTerm { get; init; }
}

/// <summary>
/// Extension methods for applying pagination to IQueryable
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Apply pagination to a queryable source
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, PaginationRequest request)
    {
        return query
            .Skip(request.Skip)
            .Take(request.Take);
    }
}