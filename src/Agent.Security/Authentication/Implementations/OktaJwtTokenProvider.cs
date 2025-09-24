using Agent.Security.Authentication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Agent.Security.Authentication.Implementations;

/// <summary>
/// Okta-based JWT token provider for production environments
/// Validates tokens against Okta and retrieves user information
/// </summary>
public class OktaJwtTokenProvider : IJwtTokenProvider
{
    private readonly ILogger<OktaJwtTokenProvider> _logger;
    private readonly OktaJwtOptions _options;
    private readonly TokenValidationOptions _validationOptions;
    private readonly HttpClient _httpClient;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private TokenValidationParameters? _validationParameters;

    public OktaJwtTokenProvider(
        ILogger<OktaJwtTokenProvider> logger,
        IOptions<JwtAuthenticationOptions> options,
        HttpClient httpClient)
    {
        _logger = logger;
        var jwtOptions = options.Value;
        _options = jwtOptions.Okta ?? throw new InvalidOperationException("Okta JWT options are required");
        _validationOptions = jwtOptions.TokenValidation;
        _httpClient = httpClient;
        _tokenHandler = new JwtSecurityTokenHandler();

        _logger.LogInformation("Initialized Okta JWT token provider with domain: {Domain}", _options.Domain);
    }

    public Task<string> GenerateTokenAsync(IEnumerable<Claim> claims, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        // Token generation is handled by Okta, not by the application
        throw new NotSupportedException("Token generation is not supported for Okta provider. Tokens are issued by Okta.");
    }

    public async Task<JwtValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            // Initialize validation parameters if not done yet
            if (_validationParameters == null)
            {
                await InitializeValidationParametersAsync(cancellationToken);
            }

            var principal = _tokenHandler.ValidateToken(token, _validationParameters!, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return new JwtValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid token format"
                };
            }

            // Extract standard claims
            var userId = principal.FindFirst(_validationOptions.UserIdClaimType)?.Value;
            var roles = principal.FindAll(_validationOptions.RoleClaimType).Select(c => c.Value).ToList();

            // Okta typically uses 'groups' claim for roles if not configured otherwise
            if (!roles.Any())
            {
                roles = principal.FindAll("groups").Select(c => c.Value).ToList();
            }

            _logger.LogDebug("Successfully validated Okta JWT token for user: {UserId}", userId);

            return new JwtValidationResult
            {
                IsValid = true,
                Claims = principal.Claims,
                ExpiresAt = jwtToken.ValidTo,
                UserId = userId,
                Roles = roles
            };
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning("Okta JWT token expired: {Message}", ex.Message);
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Okta JWT token validation failed: {Message}", ex.Message);
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating Okta JWT token");
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token validation failed"
            };
        }
    }

    public async Task<UserInfo?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.ApiToken))
            {
                _logger.LogWarning("Okta API token not configured, cannot retrieve user info");
                return null;
            }

            // Call Okta Users API to get user information
            var userEndpoint = $"{_options.Domain}/api/v1/users/{userId}";

            using var request = new HttpRequestMessage(HttpMethod.Get, userEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SSWS", _options.ApiToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("User not found in Okta: {UserId}", userId);
                    return null;
                }

                _logger.LogError("Failed to get user info from Okta. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var oktaUser = JsonSerializer.Deserialize<OktaUser>(jsonContent);

            if (oktaUser == null)
            {
                _logger.LogError("Failed to deserialize Okta user response");
                return null;
            }

            // Get user groups/roles
            var groups = await GetUserGroupsAsync(userId, cancellationToken);

            _logger.LogDebug("Retrieved user info from Okta for user: {UserId}", userId);

            return new UserInfo
            {
                Id = oktaUser.Id,
                Username = oktaUser.Profile.Login,
                Email = oktaUser.Profile.Email,
                DisplayName = $"{oktaUser.Profile.FirstName} {oktaUser.Profile.LastName}".Trim(),
                Roles = groups,
                IsActive = oktaUser.Status == "ACTIVE",
                Attributes = new Dictionary<string, object>
                {
                    ["source"] = "okta",
                    ["provider"] = "okta",
                    ["status"] = oktaUser.Status,
                    ["created"] = oktaUser.Created,
                    ["lastLogin"] = oktaUser.LastLogin ?? DateTime.MinValue
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user info from Okta for user: {UserId}", userId);
            return null;
        }
    }

    private async Task InitializeValidationParametersAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get Okta's public keys for token validation
            var keysEndpoint = $"{_options.Domain}/oauth2/{_options.AuthorizationServerId}/v1/keys";
            var keysResponse = await _httpClient.GetAsync(keysEndpoint, cancellationToken);

            if (!keysResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to get Okta public keys. Status: {keysResponse.StatusCode}");
            }

            var keysJson = await keysResponse.Content.ReadAsStringAsync(cancellationToken);
            var keys = new JsonWebKeySet(keysJson);

            _validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = _validationOptions.RequireSignedTokens,
                IssuerSigningKeys = keys.Keys,
                ValidateIssuer = _options.ValidateIssuer,
                ValidIssuer = $"{_options.Domain}/oauth2/{_options.AuthorizationServerId}",
                ValidateAudience = _options.ValidateAudience,
                ValidAudiences = _options.ValidAudiences,
                ValidateLifetime = _validationOptions.ValidateLifetime,
                RequireExpirationTime = _validationOptions.RequireExpirationTime,
                ClockSkew = _validationOptions.ClockSkew,
                RoleClaimType = _validationOptions.RoleClaimType,
                NameClaimType = _validationOptions.NameClaimType
            };

            _logger.LogDebug("Initialized Okta token validation parameters");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Okta token validation parameters");
            throw;
        }
    }

    private async Task<List<string>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.ApiToken))
            {
                return new List<string>();
            }

            var groupsEndpoint = $"{_options.Domain}/api/v1/users/{userId}/groups";

            using var request = new HttpRequestMessage(HttpMethod.Get, groupsEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SSWS", _options.ApiToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get user groups from Okta. Status: {StatusCode}", response.StatusCode);
                return new List<string>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var groups = JsonSerializer.Deserialize<List<OktaGroup>>(jsonContent) ?? new List<OktaGroup>();

            return groups.Select(g => g.Profile.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user groups from Okta for user: {UserId}", userId);
            return new List<string>();
        }
    }
}

// Okta API response models
internal class OktaUser
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime? LastLogin { get; set; }
    public OktaUserProfile Profile { get; set; } = new();
}

internal class OktaUserProfile
{
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

internal class OktaGroup
{
    public string Id { get; set; } = string.Empty;
    public OktaGroupProfile Profile { get; set; } = new();
}

internal class OktaGroupProfile
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}