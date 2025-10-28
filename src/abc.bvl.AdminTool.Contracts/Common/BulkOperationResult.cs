namespace abc.bvl.AdminTool.Contracts.Common;

/// <summary>
/// Bulk operation result for batch create/update operations
/// Provides detailed statistics and error reporting
/// </summary>
/// <typeparam name="T">Type of items processed</typeparam>
public record BulkOperationResult<T>(
    int Created,
    int Updated,
    int Errors,
    IEnumerable<string> ErrorMessages,
    IEnumerable<T> Results)
{
    /// <summary>
    /// Total items processed (Created + Updated + Errors)
    /// </summary>
    public int TotalProcessed => Created + Updated + Errors;

    /// <summary>
    /// Indicates if operation was fully successful (no errors)
    /// </summary>
    public bool IsSuccess => Errors == 0;

    /// <summary>
    /// Success rate percentage (0-100)
    /// </summary>
    public double SuccessRate => TotalProcessed > 0 
        ? ((Created + Updated) / (double)TotalProcessed) * 100 
        : 0;

    /// <summary>
    /// Create empty bulk result
    /// </summary>
    public static BulkOperationResult<T> Empty() =>
        new(0, 0, 0, Enumerable.Empty<string>(), Enumerable.Empty<T>());

    /// <summary>
    /// Create success result
    /// </summary>
    public static BulkOperationResult<T> Success(
        int created, 
        int updated, 
        IEnumerable<T> results) =>
        new(created, updated, 0, Enumerable.Empty<string>(), results);
}
