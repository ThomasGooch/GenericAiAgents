using Agent.Security.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Agent.Security.Authorization;

/// <summary>
/// Extension methods for configuring authorization services
/// </summary>
public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds RBAC authorization services with predefined policies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Optional configuration for authorization options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAgentAuthorization(
        this IServiceCollection services,
        Action<AuthorizationOptions>? configureOptions = null)
    {
        services.AddAuthorizationCore(options =>
        {
            // Configure standard Agent authorization policies
            AuthorizationPolicies.ConfigurePolicies(options);

            // Allow additional configuration
            configureOptions?.Invoke(options);
        });

        // Add custom authorization handlers
        services.AddSingleton<IAuthorizationHandler, ResourceAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

    /// <summary>
    /// Adds RBAC authorization with JWT authentication integration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureAuthorization">Optional configuration for authorization options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAgentSecurityWithRBAC(
        this IServiceCollection services,
        Action<AuthorizationOptions>? configureAuthorization = null)
    {
        // Add authorization services
        services.AddAgentAuthorization(configureAuthorization);

        return services;
    }
}