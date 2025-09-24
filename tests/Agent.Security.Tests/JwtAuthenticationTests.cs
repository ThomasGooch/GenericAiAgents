using Agent.Security.Authentication;
using Agent.Security.Authentication.Implementations;
using Agent.Security.Authentication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace Agent.Security.Tests;

public class JwtAuthenticationTests : IDisposable
{
    private readonly ILogger<LocalJwtTokenProvider> _logger;
    private readonly LocalJwtTokenProvider _tokenProvider;
    private readonly JwtAuthenticationOptions _options;

    public JwtAuthenticationTests()
    {
        _logger = Substitute.For<ILogger<LocalJwtTokenProvider>>();
        _options = new JwtAuthenticationOptions
        {
            ProviderType = JwtProviderType.Local,
            Local = new LocalJwtOptions
            {
                SigningKey = "this-is-a-very-secure-signing-key-for-testing-purposes-only-do-not-use-in-production",
                Issuer = "test-issuer",
                Audience = "test-audience",
                DefaultExpiration = TimeSpan.FromHours(1)
            },
            TokenValidation = new TokenValidationOptions
            {
                RequireSignedTokens = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }
        };

        var optionsMock = Options.Create(_options);
        _tokenProvider = new LocalJwtTokenProvider(_logger, optionsMock);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithValidClaims_ShouldReturnToken()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-123"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "admin")
        };

        // Act
        var token = await _tokenProvider.GenerateTokenAsync(claims);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        // JWT tokens have 3 parts separated by dots
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnValidResult()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-456"),
            new Claim(ClaimTypes.Name, "Test User 2"),
            new Claim(ClaimTypes.Role, "user"),
            new Claim(ClaimTypes.Role, "editor")
        };

        var token = await _tokenProvider.GenerateTokenAsync(claims);

        // Act
        var result = await _tokenProvider.ValidateTokenAsync(token);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Claims);
        Assert.NotEmpty(result.Claims);
        Assert.Equal("test-user-456", result.UserId);
        Assert.Contains("user", result.Roles);
        Assert.Contains("editor", result.Roles);
        Assert.NotNull(result.ExpiresAt);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnInvalidResult()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var result = await _tokenProvider.ValidateTokenAsync(invalidToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Empty(result.Claims);
        Assert.Null(result.UserId);
        Assert.Empty(result.Roles);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ShouldReturnInvalidResult()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-789"),
            new Claim(ClaimTypes.Name, "Test User 3")
        };

        // Generate token with very short expiration
        var token = await _tokenProvider.GenerateTokenAsync(claims, TimeSpan.FromMilliseconds(50));

        // Wait for token to expire
        await Task.Delay(200);

        // Act
        var result = await _tokenProvider.ValidateTokenAsync(token);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("expired", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetUserInfoAsync_WithUserId_ShouldReturnLocalUserInfo()
    {
        // Arrange
        var userId = "local-test-user";

        // Act
        var userInfo = await _tokenProvider.GetUserInfoAsync(userId);

        // Assert
        Assert.NotNull(userInfo);
        Assert.Equal(userId, userInfo.Id);
        Assert.Equal(userId, userInfo.Username);
        Assert.Equal($"{userId}@local.dev", userInfo.Email);
        Assert.Contains("Local User", userInfo.DisplayName);
        Assert.Contains("user", userInfo.Roles);
        Assert.True(userInfo.IsActive);
        Assert.Equal("local", userInfo.Attributes["source"]);
    }

    [Fact]
    public async Task GenerateAndValidateTokenRoundTrip_ShouldPreserveClaims()
    {
        // Arrange
        var originalClaims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "roundtrip-user"),
            new Claim(ClaimTypes.Name, "Roundtrip User"),
            new Claim(ClaimTypes.Email, "roundtrip@test.com"),
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "superuser"),
            new Claim("custom-claim", "custom-value")
        };

        // Act
        var token = await _tokenProvider.GenerateTokenAsync(originalClaims, TimeSpan.FromHours(2));
        var validationResult = await _tokenProvider.ValidateTokenAsync(token);

        // Assert
        Assert.True(validationResult.IsValid);

        var validatedClaims = validationResult.Claims.ToList();

        // Check that key claims are preserved
        Assert.Equal("roundtrip-user", validationResult.UserId);
        Assert.Contains("admin", validationResult.Roles);
        Assert.Contains("superuser", validationResult.Roles);

        // Check that custom claims are preserved
        var customClaim = validatedClaims.FirstOrDefault(c => c.Type == "custom-claim");
        Assert.NotNull(customClaim);
        Assert.Equal("custom-value", customClaim.Value);
    }

    public void Dispose()
    {
        // Clean up if needed
    }
}