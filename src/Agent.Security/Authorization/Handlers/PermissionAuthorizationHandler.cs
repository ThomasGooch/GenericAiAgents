using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Agent.Security.Authorization;

/// <summary>
/// Authorization handler for permission-based authorization
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement)
    {
        try
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user ID found in claims for permission authorization");
                context.Fail();
                return Task.CompletedTask;
            }

            // Admin users have all permissions
            if (context.User.IsInRole(AgentRoles.Admin))
            {
                _logger.LogDebug("Admin user {UserId} granted permission {Permission}",
                    userId, requirement.Permission);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Check if user has the specific permission
            if (context.User.HasClaim("permission", requirement.Permission))
            {
                _logger.LogDebug("User {UserId} has required permission {Permission}",
                    userId, requirement.Permission);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Check for role-based permissions
            if (HasRoleBasedPermission(context.User, requirement.Permission))
            {
                _logger.LogDebug("User {UserId} has role-based permission {Permission}",
                    userId, requirement.Permission);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            _logger.LogWarning("User {UserId} denied permission {Permission}",
                userId, requirement.Permission);
            context.Fail();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during permission authorization for user {UserId} and permission {Permission}",
                context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                requirement.Permission);
            context.Fail();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Checks if the user has permission based on their role
    /// </summary>
    private static bool HasRoleBasedPermission(System.Security.Claims.ClaimsPrincipal user, string permission)
    {
        // Service accounts have specific system permissions
        if (user.IsInRole(AgentRoles.Service))
        {
            return permission switch
            {
                AgentPermissions.Workflow.Execute => true,
                AgentPermissions.System.ViewMetrics => true,
                AgentPermissions.System.ViewLogs => true,
                _ => false
            };
        }

        // Regular users have basic permissions
        if (user.IsInRole(AgentRoles.User))
        {
            return permission switch
            {
                AgentPermissions.Workflow.Read => true,
                AgentPermissions.Workflow.Create => true,
                AgentPermissions.System.ViewMetrics => true,
                _ => false
            };
        }

        return false;
    }
}

/// <summary>
/// Authorization requirement for permission-based operations
/// </summary>
public class PermissionAuthorizationRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionAuthorizationRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}

/// <summary>
/// Helper class for creating authorization requirements
/// </summary>
public static class AuthorizationRequirements
{
    /// <summary>
    /// Creates a permission-based authorization requirement
    /// </summary>
    /// <param name="permission">Required permission</param>
    /// <returns>Permission authorization requirement</returns>
    public static PermissionAuthorizationRequirement RequirePermission(string permission)
    {
        return new PermissionAuthorizationRequirement(permission);
    }

    /// <summary>
    /// Creates a resource-based authorization requirement
    /// </summary>
    /// <param name="operation">Required operation</param>
    /// <returns>Resource authorization requirement</returns>
    public static ResourceAuthorizationRequirement RequireResourceAccess(string operation)
    {
        return new ResourceAuthorizationRequirement(operation);
    }
}