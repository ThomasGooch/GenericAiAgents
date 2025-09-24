using Agent.Security.Authentication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Agent.Security.Authentication.Implementations;

/// <summary>
/// Local JWT token provider for development and testing
/// Generates and validates tokens using a local signing key
/// </summary>
public class LocalJwtTokenProvider : IJwtTokenProvider
{
    private readonly ILogger<LocalJwtTokenProvider> _logger;
    private readonly LocalJwtOptions _options;
    private readonly TokenValidationOptions _validationOptions;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public LocalJwtTokenProvider(
        ILogger<LocalJwtTokenProvider> logger,
        IOptions<JwtAuthenticationOptions> options)
    {
        _logger = logger;
        var jwtOptions = options.Value;
        _options = jwtOptions.Local ?? throw new InvalidOperationException("Local JWT options are required");
        _validationOptions = jwtOptions.TokenValidation;
        
        _tokenHandler = new JwtSecurityTokenHandler();
        
        // Create signing credentials
        var key = Encoding.UTF8.GetBytes(_options.SigningKey);
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature);

        // Create validation parameters
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = _validationOptions.RequireSignedTokens,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = _validationOptions.ValidateLifetime,
            RequireExpirationTime = _validationOptions.RequireExpirationTime,
            ClockSkew = _validationOptions.ClockSkew,
            RoleClaimType = _validationOptions.RoleClaimType,
            NameClaimType = _validationOptions.NameClaimType
        };

        _logger.LogInformation("Initialized Local JWT token provider with issuer: {Issuer}", _options.Issuer);
    }

    public async Task<string> GenerateTokenAsync(IEnumerable<Claim> claims, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenExpiration = expiration ?? _options.DefaultExpiration;
            var expirationTime = DateTime.UtcNow.Add(tokenExpiration);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expirationTime,
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                SigningCredentials = _signingCredentials
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = _tokenHandler.WriteToken(token);

            _logger.LogDebug("Generated JWT token with expiration: {Expiration}", expirationTime);

            return await Task.FromResult(tokenString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWT token");
            throw;
        }
    }

    public async Task<JwtValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return new JwtValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid token format"
                };
            }

            var userId = principal.FindFirst(_validationOptions.UserIdClaimType)?.Value;
            var roles = principal.FindAll(_validationOptions.RoleClaimType).Select(c => c.Value).ToList();

            _logger.LogDebug("Successfully validated JWT token for user: {UserId}", userId);

            return await Task.FromResult(new JwtValidationResult
            {
                IsValid = true,
                Claims = principal.Claims,
                ExpiresAt = jwtToken.ValidTo,
                UserId = userId,
                Roles = roles
            });
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning("JWT token expired: {Message}", ex.Message);
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("JWT token validation failed: {Message}", ex.Message);
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating JWT token");
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token validation failed"
            };
        }
    }

    public async Task<UserInfo?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        // For local JWT provider, we don't have an external user store
        // Return basic user info based on user ID
        _logger.LogDebug("Getting user info for local user: {UserId}", userId);

        return await Task.FromResult(new UserInfo
        {
            Id = userId,
            Username = userId,
            Email = $"{userId}@local.dev",
            DisplayName = $"Local User {userId}",
            Roles = new[] { "user" },
            IsActive = true,
            Attributes = new Dictionary<string, object>
            {
                ["source"] = "local",
                ["provider"] = "local-jwt"
            }
        });
    }
}