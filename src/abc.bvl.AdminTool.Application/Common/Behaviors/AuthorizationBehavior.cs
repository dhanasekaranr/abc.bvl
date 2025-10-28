using abc.bvl.AdminTool.Application.Common.Interfaces;
using MediatR;
using System.Reflection;

namespace abc.bvl.AdminTool.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that enforces resource-based authorization
/// Runs BEFORE command handlers execute
/// Checks database-driven permissions for write operations
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUserPermissionService _permissionService;
    private readonly IRequestContext _requestContext;

    public AuthorizationBehavior(
        IUserPermissionService permissionService,
        IRequestContext requestContext)
    {
        _permissionService = permissionService;
        _requestContext = requestContext;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        // Only check authorization for commands (write operations)
        // Queries are handled by controller-level [Authorize] attribute
        var isCommand = IsCommand(request);
        
        if (!isCommand)
        {
            // This is a query - skip authorization check
            return await next();
        }

        // Extract resource type from command name
        var resourceType = GetResourceType(request);
        var userId = _requestContext.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // Check database permissions
        var permissions = await _permissionService.GetUserPermissionsAsync(
            userId, 
            resourceType, 
            cancellationToken);

        if (!permissions.CanWrite)
        {
            throw new UnauthorizedAccessException(
                $"User '{userId}' does not have write access to {resourceType}. {permissions.Reason}");
        }

        // User is authorized - proceed with command execution
        return await next();
    }

    /// <summary>
    /// Determine if request is a command (write operation) or query (read operation)
    /// Commands typically end with "Command"
    /// </summary>
    private bool IsCommand(TRequest request)
    {
        var requestType = request.GetType();
        return requestType.Name.EndsWith("Command", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extract resource type from command name
    /// Example: CreateScreenDefinitionCommand -> ScreenDefinition
    /// </summary>
    private string GetResourceType(TRequest request)
    {
        var requestType = request.GetType();
        var typeName = requestType.Name;

        // Remove Command suffix and operation prefix (Create/Update/Delete/Upsert)
        var resourceName = typeName
            .Replace("Command", "")
            .Replace("Create", "")
            .Replace("Update", "")
            .Replace("Delete", "")
            .Replace("Upsert", "");

        return resourceName;
    }
}
