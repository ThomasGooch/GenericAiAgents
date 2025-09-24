using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Agent.Security.Authorization;

/// <summary>
/// Authorization handler for resource-based authorization
/// </summary>
public class ResourceAuthorizationHandler : AuthorizationHandler<ResourceAuthorizationRequirement, IResource>
{
    private readonly ILogger<ResourceAuthorizationHandler> _logger;

    public ResourceAuthorizationHandler(ILogger<ResourceAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceAuthorizationRequirement requirement,
        IResource resource)
    {
        try
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user ID found in claims for resource authorization");
                context.Fail();
                return Task.CompletedTask;
            }

            // Check if user owns the resource
            if (resource.OwnerId == userId)
            {
                _logger.LogDebug("User {UserId} authorized to access owned resource {ResourceId}", 
                    userId, resource.Id);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Check if user has Admin role (can access any resource)
            if (context.User.IsInRole(AgentRoles.Admin))
            {
                _logger.LogDebug("Admin user {UserId} authorized to access resource {ResourceId}", 
                    userId, resource.Id);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Check if user has Service role and resource allows service access
            if (context.User.IsInRole(AgentRoles.Service) && resource.AllowServiceAccess)
            {
                _logger.LogDebug("Service user {UserId} authorized to access resource {ResourceId}", 
                    userId, resource.Id);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Check specific resource permissions
            var requiredPermission = $"{resource.ResourceType}:{requirement.Operation}";
            if (context.User.HasClaim("permission", requiredPermission))
            {
                _logger.LogDebug("User {UserId} has permission {Permission} for resource {ResourceId}", 
                    userId, requiredPermission, resource.Id);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            _logger.LogWarning("User {UserId} denied access to resource {ResourceId} (operation: {Operation})", 
                userId, resource.Id, requirement.Operation);
            context.Fail();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resource authorization for user {UserId} and resource {ResourceId}", 
                context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
                resource.Id);
            context.Fail();
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Authorization requirement for resource-based operations
/// </summary>
public class ResourceAuthorizationRequirement : IAuthorizationRequirement
{
    public string Operation { get; }

    public ResourceAuthorizationRequirement(string operation)
    {
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
    }
}

/// <summary>
/// Interface for resources that can be authorized
/// </summary>
public interface IResource
{
    /// <summary>
    /// Unique identifier of the resource
    /// </summary>
    string Id { get; }

    /// <summary>
    /// ID of the user who owns this resource
    /// </summary>
    string? OwnerId { get; }

    /// <summary>
    /// Type of resource (e.g., "workflow", "config", "secret")
    /// </summary>
    string ResourceType { get; }

    /// <summary>
    /// Whether service accounts can access this resource
    /// </summary>
    bool AllowServiceAccess { get; }
}