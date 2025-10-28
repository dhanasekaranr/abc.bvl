using abc.bvl.AdminTool.Infrastructure.Data.Context;
using Microsoft.Extensions.Options;

namespace abc.bvl.AdminTool.Api.Services;

/// <summary>
/// Resolves the appropriate DbContext (Primary or Secondary) based on request headers
/// </summary>
public interface IDbContextResolver
{
    /// <summary>
    /// Gets the DbContext based on the X-Database header or defaults to Primary
    /// </summary>
    /// <param name="databaseName">Database name from request (optional)</param>
    /// <returns>AdminDbContext instance for the specified database</returns>
    AdminDbContext GetDbContext(string? databaseName = null);
    
    /// <summary>
    /// Gets the database name from the current request context
    /// </summary>
    string GetCurrentDatabase();
}

public class DbContextResolver : IDbContextResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DbContextResolver> _logger;

    public DbContextResolver(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        ILogger<DbContextResolver> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public AdminDbContext GetDbContext(string? databaseName = null)
    {
        // Priority: 1. Parameter, 2. Request Header, 3. Default to Primary
        var dbName = databaseName ?? GetCurrentDatabase();

        _logger.LogDebug("Resolving DbContext for database: {DatabaseName}", dbName);

        // Resolve the appropriate DbContext from DI
        var contextKey = dbName.Equals("Secondary", StringComparison.OrdinalIgnoreCase) 
            ? "Secondary" 
            : "Primary";

        try
        {
            // Get the named DbContext from DI container
            var dbContext = _serviceProvider.GetRequiredKeyedService<AdminDbContext>(contextKey);
            
            _logger.LogInformation("Resolved {DbContextType} DbContext", contextKey);
            return dbContext;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to resolve {DbContextType} DbContext, falling back to Primary", contextKey);
            
            // Fallback to Primary if resolution fails
            return _serviceProvider.GetRequiredKeyedService<AdminDbContext>("Primary");
        }
    }

    public string GetCurrentDatabase()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return "Primary"; // Default when no HTTP context (background tasks, etc.)
        }

        // Check for X-Database header
        if (httpContext.Request.Headers.TryGetValue("X-Database", out var databaseHeader))
        {
            var dbValue = databaseHeader.ToString();
            if (!string.IsNullOrWhiteSpace(dbValue))
            {
                _logger.LogDebug("Database routing header found: {Database}", dbValue);
                return dbValue;
            }
        }

        // Check for query parameter (alternative method)
        if (httpContext.Request.Query.TryGetValue("database", out var databaseQuery))
        {
            var dbValue = databaseQuery.ToString();
            if (!string.IsNullOrWhiteSpace(dbValue))
            {
                _logger.LogDebug("Database routing query parameter found: {Database}", dbValue);
                return dbValue;
            }
        }

        return "Primary"; // Default
    }
}
