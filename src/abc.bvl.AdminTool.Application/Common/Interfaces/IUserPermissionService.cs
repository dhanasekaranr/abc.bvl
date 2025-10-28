namespace abc.bvl.AdminTool.Application.Common.Interfaces;

/// <summary>
/// Service for checking user permissions from database
/// </summary>
public interface IUserPermissionService
{
    /// <summary>
    /// Check if user has write access to a specific resource
    /// </summary>
    Task<bool> HasWriteAccessAsync(string userId, string resourceType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if user has read access to a specific resource
    /// </summary>
    Task<bool> HasReadAccessAsync(string userId, string resourceType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get user's effective permissions for a resource
    /// </summary>
    Task<UserPermissions> GetUserPermissionsAsync(string userId, string resourceType, CancellationToken cancellationToken = default);
}

/// <summary>
/// User's permissions for a specific resource
/// </summary>
public record UserPermissions(
    bool CanRead,
    bool CanWrite,
    bool CanDelete,
    string[] Roles,
    string? Reason = null
);
