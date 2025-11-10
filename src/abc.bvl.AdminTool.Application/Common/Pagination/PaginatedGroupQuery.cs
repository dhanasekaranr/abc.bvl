using System.Linq.Expressions;

namespace abc.bvl.AdminTool.Application.Common.Pagination;

/// <summary>
/// Generic two-phase pagination for grouped data
/// Phase 1: Get paginated group keys from DB
/// Phase 2: Load full data only for those keys
/// </summary>
/// <typeparam name="TSource">Source DTO type (e.g., ScreenPilotDto)</typeparam>
/// <typeparam name="TGroupKey">Group key type (e.g., string for UserId)</typeparam>
/// <typeparam name="TResult">Result DTO type (e.g., PilotEnablementDto)</typeparam>
public class PaginatedGroupQuery<TSource, TGroupKey, TResult> where TGroupKey : notnull
{
    private readonly IQueryable<TSource> _baseQuery;
    private readonly Expression<Func<TSource, TGroupKey>> _groupKeySelector;
    private readonly Func<IGrouping<TGroupKey, TSource>, TResult> _resultSelector;
    
    private Func<IQueryable<TGroupKey>, IQueryable<TGroupKey>>? _groupKeyFilter;
    private Func<IQueryable<TGroupKey>, IOrderedQueryable<TGroupKey>>? _groupKeyOrderBy;
    private int? _skip;
    private int? _take;

    public PaginatedGroupQuery(
        IQueryable<TSource> baseQuery,
        Expression<Func<TSource, TGroupKey>> groupKeySelector,
        Func<IGrouping<TGroupKey, TSource>, TResult> resultSelector)
    {
        _baseQuery = baseQuery ?? throw new ArgumentNullException(nameof(baseQuery));
        _groupKeySelector = groupKeySelector ?? throw new ArgumentNullException(nameof(groupKeySelector));
        _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
    }

    /// <summary>
    /// Add filter to group keys (e.g., filter by specific UserId)
    /// </summary>
    public PaginatedGroupQuery<TSource, TGroupKey, TResult> WhereGroupKey(
        Func<IQueryable<TGroupKey>, IQueryable<TGroupKey>> filter)
    {
        _groupKeyFilter = filter;
        return this;
    }

    /// <summary>
    /// Add ordering to group keys (required for consistent pagination)
    /// </summary>
    public PaginatedGroupQuery<TSource, TGroupKey, TResult> OrderGroupKeysBy(
        Func<IQueryable<TGroupKey>, IOrderedQueryable<TGroupKey>> orderBy)
    {
        _groupKeyOrderBy = orderBy;
        return this;
    }

    /// <summary>
    /// Set pagination parameters
    /// </summary>
    public PaginatedGroupQuery<TSource, TGroupKey, TResult> Paginate(int page, int pageSize)
    {
        _skip = (page - 1) * pageSize;
        _take = pageSize;
        return this;
    }

    /// <summary>
    /// Execute the two-phase query and return results
    /// </summary>
    public List<TResult> Execute()
    {
        // Phase 1: Get distinct group keys with pagination
        IQueryable<TGroupKey> groupKeysQuery = _baseQuery
            .Select(_groupKeySelector)
            .Distinct();

        // Apply group key ordering (required for pagination)
        if (_groupKeyOrderBy != null)
        {
            groupKeysQuery = _groupKeyOrderBy(groupKeysQuery);
        }

        // Apply group key filter
        if (_groupKeyFilter != null)
        {
            groupKeysQuery = _groupKeyFilter(groupKeysQuery);
        }

        // Apply pagination to group keys
        if (_skip.HasValue)
        {
            groupKeysQuery = groupKeysQuery.Skip(_skip.Value);
        }
        if (_take.HasValue)
        {
            groupKeysQuery = groupKeysQuery.Take(_take.Value);
        }

        // Execute query - get paginated group keys
        var groupKeys = groupKeysQuery.ToList();

        if (!groupKeys.Any())
        {
            return new List<TResult>();
        }

        // Phase 2: Load full data only for paginated group keys
        var compiledSelector = _groupKeySelector.Compile();
        var dataForKeys = _baseQuery
            .ToList() // Execute second query
            .Where(item => groupKeys.Contains(compiledSelector(item)))
            .ToList();

        // Group and transform
        var results = dataForKeys
            .GroupBy(compiledSelector)
            .Select(_resultSelector)
            .ToList();

        return results;
    }

    /// <summary>
    /// Execute async version
    /// </summary>
    public async Task<List<TResult>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Note: ToListAsync requires EF Core reference in Application layer
        // For now, use synchronous execution
        // TODO: Consider moving to Infrastructure or adding EF Core dependency
        return await Task.Run(() => Execute(), cancellationToken);
    }
}
