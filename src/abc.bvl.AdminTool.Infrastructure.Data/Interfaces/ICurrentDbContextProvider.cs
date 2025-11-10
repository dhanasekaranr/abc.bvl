using abc.bvl.AdminTool.Infrastructure.Data.Context;

namespace abc.bvl.AdminTool.Infrastructure.Data.Interfaces;

/// <summary>
/// Provides access to the current request-scoped database context
/// Used for dual-database routing based on HTTP headers
/// </summary>
public interface ICurrentDbContextProvider
{
    /// <summary>
    /// Gets the current database context for this request
    /// </summary>
    /// <returns>The appropriate AdminDbContext (Primary or Secondary)</returns>
    AdminDbContext GetContext();

    /// <summary>
    /// Sets the database context type for the current request
    /// Called by middleware based on routing headers
    /// </summary>
    /// <param name="contextType">The type of context to use (Primary or Secondary)</param>
    void SetContextType(DatabaseContextType contextType);
}

/// <summary>
/// Enum for database context type selection
/// </summary>
public enum DatabaseContextType
{
    Primary,
    Secondary
}
