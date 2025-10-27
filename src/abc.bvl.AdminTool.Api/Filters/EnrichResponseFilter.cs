using abc.bvl.AdminTool.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace abc.bvl.AdminTool.Api.Filters;

/// <summary>
/// Automatically enriches all API responses with UserInfo and AccessInfo from JWT claims
/// This eliminates the need to manually populate BasePageDto metadata in every controller
/// </summary>
public class EnrichResponseFilter : IResultFilter
{
    private readonly ILogger<EnrichResponseFilter> _logger;

    public EnrichResponseFilter(ILogger<EnrichResponseFilter> logger)
    {
        _logger = logger;
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        try
        {
            // Only process successful responses (200 OK)
            if (context.Result is OkObjectResult okResult && okResult.Value != null)
            {
                var value = okResult.Value;
                
                // Check if the response is a BasePageDto derivative
                if (value is BasePageDto basePageDto)
                {
                    var enrichedDto = EnrichBasePageDto(basePageDto, context.HttpContext);
                    
                    // Replace the result value with enriched version
                    okResult.Value = enrichedDto;
                    
                    _logger.LogDebug("Enriched response with UserInfo and AccessInfo for {Path}", 
                        context.HttpContext.Request.Path);
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the request if enrichment fails
            _logger.LogWarning(ex, "Failed to enrich response for {Path}. Response will be returned without enrichment.",
                context.HttpContext.Request.Path);
            
            // Don't throw - let the response through without enrichment
            // This ensures the API remains functional even if enrichment fails
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // Nothing to do after result execution
    }

    /// <summary>
    /// Enriches BasePageDto with UserInfo and AccessInfo from HttpContext
    /// Uses 'with' expression to create new instance (immutable records)
    /// Safely handles anonymous/invalid users without throwing exceptions
    /// </summary>
    private BasePageDto EnrichBasePageDto(BasePageDto dto, HttpContext httpContext)
    {
        var user = httpContext.User;
        
        // Safely extract UserInfo from JWT claims (handles anonymous users gracefully)
        var userId = GetClaimValue(user, ClaimTypes.NameIdentifier, "sub");
        var displayName = GetClaimValue(user, ClaimTypes.Name, "name");
        var email = GetClaimValue(user, ClaimTypes.Email, "email");
        
        // Check if user is authenticated
        var isAuthenticated = user?.Identity?.IsAuthenticated ?? false;
        
        var userInfo = new UserInfo(
            UserId: userId ?? (isAuthenticated ? "authenticated-user" : "anonymous"),
            DisplayName: displayName ?? (isAuthenticated ? "Authenticated User" : "Anonymous User"),
            Email: email ?? (isAuthenticated ? "user@example.com" : "anonymous@example.com")
        );

        // Extract AccessInfo from JWT claims and headers (safe for anonymous users)
        var roles = user?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToArray() ?? Array.Empty<string>();
        
        var dbRoute = httpContext.Request.Headers.TryGetValue("X-DB-Route", out var route) 
            ? route.ToString() 
            : "primary";

        var accessInfo = new AccessInfo(
            CanRead: isAuthenticated, // Anonymous users can't read by default
            CanWrite: isAuthenticated && (user!.IsInRole("Admin") || user.IsInRole("Editor")),
            Roles: roles,
            DbRoute: dbRoute
        );

        // Use 'with' expression to create enriched copy (works with records)
        return dto with
        {
            User = userInfo,
            Access = accessInfo,
            CorrelationId = httpContext.TraceIdentifier,
            ServerTime = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Gets claim value, trying multiple claim types (standard and OIDC)
    /// </summary>
    private string? GetClaimValue(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var claim = user.FindFirst(claimType);
            if (claim != null && !string.IsNullOrEmpty(claim.Value))
                return claim.Value;
        }
        return null;
    }
}
