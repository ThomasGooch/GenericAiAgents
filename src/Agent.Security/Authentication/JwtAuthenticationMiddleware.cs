using Agent.Security.Authentication.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Agent.Security.Authentication;

/// <summary>
/// Middleware for JWT authentication that works with various providers (Local, Okta, Azure AD)
/// </summary>
public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly JwtAuthenticationOptions _options;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger,
        IOptions<JwtAuthenticationOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health check endpoints
        if (IsHealthCheckEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Extract token from Authorization header
        var token = ExtractToken(context.Request);

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogDebug("No JWT token found in request to {Path}", context.Request.Path);
            await _next(context);
            return;
        }

        try
        {
            // Get the JWT token provider from DI
            var tokenProvider = context.RequestServices.GetRequiredService<IJwtTokenProvider>();

            // Validate the token
            var validationResult = await tokenProvider.ValidateTokenAsync(token, context.RequestAborted);

            if (validationResult.IsValid)
            {
                // Create claims identity and set user context
                var identity = new ClaimsIdentity(validationResult.Claims, "jwt");
                context.User = new ClaimsPrincipal(identity);

                // Add user information to context for easy access
                context.Items["UserId"] = validationResult.UserId;
                context.Items["UserRoles"] = validationResult.Roles;
                context.Items["TokenExpiration"] = validationResult.ExpiresAt;

                _logger.LogDebug("Successfully authenticated user {UserId} for request to {Path}",
                    validationResult.UserId, context.Request.Path);
            }
            else
            {
                _logger.LogWarning("JWT token validation failed for request to {Path}: {Error}",
                    context.Request.Path, validationResult.ErrorMessage);

                // For invalid tokens, we don't set the user context
                // The authorization middleware will handle the unauthorized response
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during JWT authentication for request to {Path}", context.Request.Path);
            // Continue processing - authorization middleware will handle the unauthorized state
        }

        await _next(context);
    }

    private string? ExtractToken(HttpRequest request)
    {
        // Check Authorization header first
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring(7); // Remove "Bearer " prefix
        }

        // Check query parameter (for WebSocket connections or special cases)
        var queryToken = request.Query["access_token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(queryToken))
        {
            return queryToken;
        }

        // Check cookie (if configured)
        var cookieToken = request.Cookies["access_token"];
        if (!string.IsNullOrEmpty(cookieToken))
        {
            return cookieToken;
        }

        return null;
    }

    private static bool IsHealthCheckEndpoint(PathString path)
    {
        return path.StartsWithSegments("/health") ||
               path.StartsWithSegments("/healthz") ||
               path.StartsWithSegments("/ready") ||
               path.StartsWithSegments("/live");
    }
}

/// <summary>
/// Extension methods for adding JWT authentication middleware
/// </summary>
public static class JwtAuthenticationMiddlewareExtensions
{
    /// <summary>
    /// Adds JWT authentication middleware to the pipeline
    /// </summary>
    /// <param name="builder">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtAuthenticationMiddleware>();
    }
}

/// <summary>
/// Helper extensions for accessing JWT authentication data from HttpContext
/// </summary>
public static class HttpContextJwtExtensions
{
    /// <summary>
    /// Gets the current user ID from JWT token
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>User ID or null if not authenticated</returns>
    public static string? GetJwtUserId(this HttpContext context)
    {
        return context.Items["UserId"] as string;
    }

    /// <summary>
    /// Gets the current user roles from JWT token
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>User roles or empty list if not authenticated</returns>
    public static IEnumerable<string> GetJwtUserRoles(this HttpContext context)
    {
        return (context.Items["UserRoles"] as IEnumerable<string>) ?? Enumerable.Empty<string>();
    }

    /// <summary>
    /// Gets the JWT token expiration time
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Token expiration time or null if not authenticated</returns>
    public static DateTime? GetJwtTokenExpiration(this HttpContext context)
    {
        return context.Items["TokenExpiration"] as DateTime?;
    }

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="role">Role to check</param>
    /// <returns>True if user has the role</returns>
    public static bool HasJwtRole(this HttpContext context, string role)
    {
        return context.GetJwtUserRoles().Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}