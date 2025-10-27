using abc.bvl.AdminTool.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace abc.bvl.AdminTool.Api.Controllers.Base;

/// <summary>
/// Base controller providing common functionality for all API controllers
/// Includes helper methods for UserInfo, AccessInfo, and standardized responses
/// </summary>
[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    protected readonly ILogger _logger;

    protected BaseApiController(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region UserInfo and AccessInfo Helpers

    /// <summary>
    /// Get user information from JWT claims
    /// This should be called automatically by EnrichResponseFilter, but available for manual use
    /// </summary>
    protected UserInfo GetUserInfo()
    {
        return new UserInfo(
            UserId: GetCurrentUserId(),
            DisplayName: GetCurrentUserName(),
            Email: GetCurrentUserEmail()
        );
    }

    /// <summary>
    /// Get access information from JWT claims and headers
    /// This should be called automatically by EnrichResponseFilter, but available for manual use
    /// </summary>
    protected AccessInfo GetAccessInfo()
    {
        return new AccessInfo(
            CanRead: User.Identity?.IsAuthenticated ?? false,
            CanWrite: User.IsInRole("Admin") || User.IsInRole("Editor"),
            Roles: GetCurrentUserRoles(),
            DbRoute: GetDatabaseRoute()
        );
    }

    /// <summary>
    /// Get the current authenticated user's ID
    /// </summary>
    protected string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               "anonymous";
    }

    /// <summary>
    /// Get the current authenticated user's display name
    /// </summary>
    protected string GetCurrentUserName()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? 
               User.FindFirst("name")?.Value ?? 
               "Unknown User";
    }

    /// <summary>
    /// Get the current authenticated user's email
    /// </summary>
    protected string GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value ?? 
               User.FindFirst("email")?.Value ?? 
               "unknown@example.com";
    }

    /// <summary>
    /// Get the current user's roles
    /// </summary>
    protected string[] GetCurrentUserRoles()
    {
        return User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();
    }

    /// <summary>
    /// Get the database route from request headers
    /// Used for dual-DB routing scenarios
    /// </summary>
    protected string GetDatabaseRoute()
    {
        return Request.Headers.TryGetValue("X-DB-Route", out var route) 
            ? route.ToString() 
            : "primary";
    }

    #endregion

    #region Standardized Response Helpers

    /// <summary>
    /// Create a standardized single item response
    /// UserInfo, AccessInfo, and CorrelationId are automatically populated by EnrichResponseFilter
    /// Controllers just focus on business data!
    /// </summary>
    protected ActionResult<SingleResult<T>> SingleSuccess<T>(T data) where T : class
    {
        var result = new SingleResult<T>(data);
        return Ok(result);
    }

    /// <summary>
    /// Create a standardized paged response
    /// UserInfo, AccessInfo, and CorrelationId are automatically populated by EnrichResponseFilter
    /// Controllers just focus on business data!
    /// </summary>
    protected ActionResult<PagedResult<T>> PagedSuccess<T>(
        IEnumerable<T> items,
        int currentPage,
        int pageSize,
        long totalItems) where T : class
    {
        var result = new PagedResult<T>(items, currentPage, pageSize, totalItems);
        return Ok(result);
    }

    /// <summary>
    /// Create a standardized error response with correlation tracking
    /// </summary>
    protected ActionResult<ApiResponse<object>> ErrorResponse(
        string message,
        int statusCode = 400)
    {
        var response = new
        {
            Error = message,
            CorrelationId = HttpContext.TraceIdentifier,
            Timestamp = DateTimeOffset.UtcNow
        };

        return StatusCode(statusCode, response);
    }

    #endregion

    #region Logging Helpers

    /// <summary>
    /// Log an operation with user context
    /// </summary>
    protected void LogOperation(string operation, object? additionalData = null)
    {
        _logger.LogInformation(
            "{Operation} by {UserId} on {Path} - {Data}",
            operation,
            GetCurrentUserId(),
            Request.Path,
            additionalData ?? "N/A"
        );
    }

    /// <summary>
    /// Log an error with user context
    /// </summary>
    protected void LogError(Exception ex, string operation)
    {
        _logger.LogError(
            ex,
            "{Operation} failed for {UserId} on {Path}",
            operation,
            GetCurrentUserId(),
            Request.Path
        );
    }

    #endregion
}
