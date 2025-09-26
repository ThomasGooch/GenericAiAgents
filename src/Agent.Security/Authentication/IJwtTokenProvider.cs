namespace Agent.Security.Authentication;

/// <summary>
/// Abstraction for JWT token validation and claims extraction
/// Works with local JWT tokens (dev/test) or external providers (Okta, Azure AD)
/// </summary>
public interface IJwtTokenProvider
{
    /// <summary>
    /// Validates a JWT token and returns claims if valid
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token validation result with claims</returns>
    Task<JwtValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a JWT token (only for local/testing scenarios)
    /// </summary>
    /// <param name="claims">Claims to include in token</param>
    /// <param name="expiration">Token expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated JWT token</returns>
    Task<string> GenerateTokenAsync(IEnumerable<System.Security.Claims.Claim> claims, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user information from the identity provider (for external providers like Okta)
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    Task<UserInfo?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the comprehensive result of JSON Web Token (JWT) validation operations, providing
/// detailed information about token validity, extracted claims, security context, and error conditions.
/// 
/// This class encapsulates all aspects of JWT token validation including cryptographic verification,
/// expiration checks, issuer validation, audience verification, and claims extraction. It serves as
/// the primary contract for communicating authentication results between JWT providers and consuming
/// applications in enterprise security scenarios.
/// 
/// The validation result supports various authentication patterns:
/// - Standard JWT validation with signature verification and standard claims validation
/// - Integration with external identity providers (Okta, Azure AD, Auth0, AWS Cognito)
/// - Custom claims processing for role-based access control (RBAC) and attribute-based access control (ABAC)
/// - Multi-tenant scenarios with tenant-specific validation rules
/// - Token introspection for OAuth 2.0 and OpenID Connect compliance
/// - Audit logging and security monitoring integration for compliance requirements
/// 
/// Security Features:
/// - Comprehensive validation including signature verification, expiration, and standard claims
/// - Support for custom validation rules and business-specific claim requirements
/// - Detailed error reporting for security monitoring and threat detection
/// - Integration with enterprise audit logging for compliance tracking
/// - Role and permission extraction for authorization decision support
/// 
/// Enterprise Integration:
/// - Compatible with Microsoft Identity Platform, Okta, and other enterprise identity providers
/// - Support for custom claim transformations and user context enrichment
/// - Integration with distributed caching for token validation result caching
/// - Metrics and observability support for authentication performance monitoring
/// </summary>
/// <example>
/// Basic JWT validation result handling:
/// <code>
/// var tokenProvider = serviceProvider.GetService&lt;IJwtTokenProvider&gt;();
/// var validationResult = await tokenProvider.ValidateTokenAsync(jwtToken);
/// 
/// if (validationResult.IsValid)
/// {
///     var userId = validationResult.UserId;
///     var userRoles = validationResult.Roles;
///     
///     // Extract custom claims for business logic
///     var tenantClaim = validationResult.Claims.FirstOrDefault(c =&gt; c.Type == "tenant_id");
///     var permissions = validationResult.Claims.Where(c =&gt; c.Type == "permissions").Select(c =&gt; c.Value);
///     
///     // Create authentication context
///     var identity = new ClaimsIdentity(validationResult.Claims, "jwt");
///     var principal = new ClaimsPrincipal(identity);
/// }
/// else
/// {
///     // Handle validation failure
///     _logger.LogWarning("JWT validation failed: {Error}", validationResult.ErrorMessage);
///     return Unauthorized();
/// }
/// </code>
/// 
/// Enterprise RBAC integration:
/// <code>
/// public class AuthenticationService
/// {
///     public async Task&lt;AuthenticationResult&gt; AuthenticateAsync(string token)
///     {
///         var validationResult = await _jwtProvider.ValidateTokenAsync(token);
///         
///         if (!validationResult.IsValid)
///         {
///             return AuthenticationResult.Failed(validationResult.ErrorMessage);
///         }
/// 
///         // Check token expiration with buffer for clock skew
///         if (validationResult.ExpiresAt &lt;= DateTime.UtcNow.AddMinutes(-5))
///         {
///             return AuthenticationResult.Failed("Token expired");
///         }
/// 
///         // Extract and validate required roles
///         var requiredRoles = new[] { "User", "Active" };
///         var hasRequiredRoles = requiredRoles.All(role =&gt; validationResult.Roles.Contains(role));
///         
///         if (!hasRequiredRoles)
///         {
///             return AuthenticationResult.Failed("Insufficient permissions");
///         }
/// 
///         return AuthenticationResult.Success(validationResult.UserId, validationResult.Roles);
///     }
/// }
/// </code>
/// 
/// Multi-tenant validation with custom claims:
/// <code>
/// public async Task&lt;bool&gt; ValidateUserAccessToTenantAsync(string token, string tenantId)
/// {
///     var validationResult = await _jwtProvider.ValidateTokenAsync(token);
///     
///     if (!validationResult.IsValid)
///     {
///         return false;
///     }
/// 
///     // Extract tenant information from JWT claims
///     var userTenantClaim = validationResult.Claims.FirstOrDefault(c =&gt; c.Type == "tenant_id");
///     var userTenantId = userTenantClaim?.Value;
/// 
///     // Validate user belongs to requested tenant
///     if (userTenantId != tenantId)
///     {
///         _logger.LogWarning("User {UserId} attempted to access tenant {TenantId} but belongs to {UserTenantId}",
///             validationResult.UserId, tenantId, userTenantId);
///         return false;
///     }
/// 
///     // Additional validation for tenant-specific roles
///     var tenantRoleClaims = validationResult.Claims.Where(c =&gt; c.Type == "tenant_roles").Select(c =&gt; c.Value);
///     return tenantRoleClaims.Any(role =&gt; role == "TenantUser" || role == "TenantAdmin");
/// }
/// </code>
/// </example>
/// <remarks>
/// Performance Considerations:
/// - Validation results should be cached when appropriate to reduce cryptographic operations
/// - Claims extraction is performed once during validation to minimize repeated parsing
/// - Consider implementing result caching for frequently validated tokens
/// 
/// Security Best Practices:
/// - Always check IsValid before trusting any other properties in the result
/// - Implement proper clock skew tolerance for expiration time validation
/// - Log validation failures for security monitoring and threat detection
/// - Validate audience and issuer claims for multi-service environments
/// 
/// Error Handling:
/// - Check ErrorMessage property for detailed failure information when IsValid is false
/// - Implement appropriate retry logic for transient validation failures
/// - Consider graceful degradation strategies for non-critical validation scenarios
/// 
/// Integration Patterns:
/// - Use with ASP.NET Core authentication middleware for seamless request authentication
/// - Integrate with authorization policies for fine-grained access control
/// - Combine with user context services for enriched authentication information
/// - Support for custom claims transformations based on business requirements
/// </remarks>
public class JwtValidationResult
{
    /// <summary>
    /// Whether the token is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Claims extracted from the token
    /// </summary>
    public IEnumerable<System.Security.Claims.Claim> Claims { get; set; } = new List<System.Security.Claims.Claim>();

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// User identifier from token
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// User roles from token
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}

/// <summary>
/// User information from identity provider
/// </summary>
public class UserInfo
{
    /// <summary>
    /// User identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// User roles
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = new List<string>();

    /// <summary>
    /// Additional claims/attributes
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();

    /// <summary>
    /// Whether user is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}