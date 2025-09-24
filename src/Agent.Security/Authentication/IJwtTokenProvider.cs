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
/// Result of JWT token validation
/// </summary>
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