using System.Linq.Expressions;

namespace abc.bvl.AdminTool.Application.Common.Pagination;

/// <summary>
/// Extension methods to make PaginatedGroupQuery easier to use
/// </summary>
public static class PaginatedGroupQueryExtensions
{
    /// <summary>
    /// Create a paginated group query from an IQueryable
    /// </summary>
    public static PaginatedGroupQuery<TSource, TGroupKey, TResult> GroupByPaginated<TSource, TGroupKey, TResult>(
        this IQueryable<TSource> query,
        Expression<Func<TSource, TGroupKey>> groupKeySelector,
        Func<IGrouping<TGroupKey, TSource>, TResult> resultSelector)
        where TGroupKey : notnull
    {
        return new PaginatedGroupQuery<TSource, TGroupKey, TResult>(
            query,
            groupKeySelector,
            resultSelector
        );
    }
}
