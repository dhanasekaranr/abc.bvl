using abc.bvl.AdminTool.Infrastructure.Data.Interfaces;

namespace abc.bvl.AdminTool.Api.Middleware;

/// <summary>
/// Middleware that reads the X-Database-Route header and configures database context routing
/// Supports routing requests to either primary (APP_USER) or secondary (CVLWEBTOOLS) database
/// </summary>
public class DatabaseRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DatabaseRoutingMiddleware> _logger;

    public DatabaseRoutingMiddleware(RequestDelegate next, ILogger<DatabaseRoutingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentDbContextProvider dbContextProvider)
    {
        // Default to primary database
        var contextType = DatabaseContextType.Primary;

        // Check for X-Database-Route header
        if (context.Request.Headers.TryGetValue("X-Database-Route", out var routeHeader))
        {
            var route = routeHeader.ToString().ToLowerInvariant();
            
            if (route == "secondary" || route == "cvlwebtools")
            {
                contextType = DatabaseContextType.Secondary;
                _logger.LogDebug("Routing request to SECONDARY database (CVLWEBTOOLS schema)");
            }
            else if (route == "primary" || route == "app_user")
            {
                contextType = DatabaseContextType.Primary;
                _logger.LogDebug("Routing request to PRIMARY database (APP_USER schema)");
            }
            else
            {
                _logger.LogWarning("Unknown X-Database-Route value '{Route}'. Defaulting to PRIMARY.", route);
            }
        }

        // Set the context type for this request
        dbContextProvider.SetContextType(contextType);

        // Continue with the request pipeline
        await _next(context);
    }
}

/// <summary>
/// Extension method for registering DatabaseRoutingMiddleware
/// </summary>
public static class DatabaseRoutingMiddlewareExtensions
{
    public static IApplicationBuilder UseDatabaseRouting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DatabaseRoutingMiddleware>();
    }
}
