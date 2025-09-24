namespace Agent.Security.Authentication.Models;

/// <summary>
/// Configuration options for JWT authentication
/// </summary>
public class JwtAuthenticationOptions
{
    /// <summary>
    /// The type of JWT provider to use
    /// </summary>
    public JwtProviderType ProviderType { get; set; } = JwtProviderType.Local;

    /// <summary>
    /// Local JWT configuration (for development/testing)
    /// </summary>
    public LocalJwtOptions? Local { get; set; }

    /// <summary>
    /// Okta configuration (for production)
    /// </summary>
    public OktaJwtOptions? Okta { get; set; }

    /// <summary>
    /// Azure AD configuration (for production)
    /// </summary>
    public AzureAdJwtOptions? AzureAd { get; set; }

    /// <summary>
    /// Token validation settings
    /// </summary>
    public TokenValidationOptions TokenValidation { get; set; } = new();

    /// <summary>
    /// Whether to require HTTPS for token validation
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Whether to cache validation results
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time for validation results
    /// </summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Types of JWT providers supported
/// </summary>
public enum JwtProviderType
{
    Local,
    Okta,
    AzureAd,
    Custom
}

/// <summary>
/// Local JWT configuration for development/testing
/// </summary>
public class LocalJwtOptions
{
    /// <summary>
    /// JWT signing key (should be from secrets in production)
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = "agent-system";

    /// <summary>
    /// Token audience
    /// </summary>
    public string Audience { get; set; } = "agent-system-api";

    /// <summary>
    /// Default token expiration
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(8);
}

/// <summary>
/// Okta-specific JWT configuration
/// </summary>
public class OktaJwtOptions
{
    /// <summary>
    /// Okta domain (e.g., https://yourcompany.okta.com)
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Okta authorization server ID (default for org auth server)
    /// </summary>
    public string AuthorizationServerId { get; set; } = "default";

    /// <summary>
    /// Client ID for your application
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret (should be from secrets)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Okta API token for user info calls
    /// </summary>
    public string? ApiToken { get; set; }

    /// <summary>
    /// Whether to validate issuer (should be true in production)
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate audience (should be true in production)
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Expected audiences (claims aud)
    /// </summary>
    public List<string> ValidAudiences { get; set; } = new();
}

/// <summary>
/// Azure AD-specific JWT configuration
/// </summary>
public class AzureAdJwtOptions
{
    /// <summary>
    /// Azure AD tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Application (client) ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret (should be from secrets)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Azure AD instance (usually https://login.microsoftonline.com)
    /// </summary>
    public string Instance { get; set; } = "https://login.microsoftonline.com";

    /// <summary>
    /// Whether to validate issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;
}

/// <summary>
/// Token validation configuration
/// </summary>
public class TokenValidationOptions
{
    /// <summary>
    /// Clock skew tolerance for token expiration
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to require token expiration
    /// </summary>
    public bool RequireExpirationTime { get; set; } = true;

    /// <summary>
    /// Whether to validate token signature
    /// </summary>
    public bool RequireSignedTokens { get; set; } = true;

    /// <summary>
    /// Whether to validate token lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Role claim type
    /// </summary>
    public string RoleClaimType { get; set; } = System.Security.Claims.ClaimTypes.Role;

    /// <summary>
    /// Name claim type  
    /// </summary>
    public string NameClaimType { get; set; } = System.Security.Claims.ClaimTypes.Name;

    /// <summary>
    /// User ID claim type
    /// </summary>
    public string UserIdClaimType { get; set; } = System.Security.Claims.ClaimTypes.NameIdentifier;
}