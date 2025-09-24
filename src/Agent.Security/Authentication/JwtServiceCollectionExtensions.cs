using Agent.Security.Authentication.Implementations;
using Agent.Security.Authentication.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Agent.Security.Authentication;

/// <summary>
/// Extension methods for registering JWT authentication services
/// </summary>
public static class JwtServiceCollectionExtensions
{
    /// <summary>
    /// Adds JWT authentication services with configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="configureOptions">Optional configuration options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtAuthenticationOptions>? configureOptions = null)
    {
        // Configure JWT options
        services.Configure<JwtAuthenticationOptions>(options => 
        {
            configuration.GetSection("JwtAuthentication").Bind(options);
            configureOptions?.Invoke(options);
        });

        // Add HTTP client for external providers
        services.AddHttpClient<IJwtTokenProvider>();

        // Register the appropriate JWT token provider based on configuration
        services.AddSingleton<IJwtTokenProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtAuthenticationOptions>>();
            var logger = serviceProvider.GetRequiredService<ILogger<IJwtTokenProvider>>();

            return options.Value.ProviderType switch
            {
                JwtProviderType.Local => serviceProvider.GetRequiredService<LocalJwtTokenProvider>(),
                JwtProviderType.Okta => serviceProvider.GetRequiredService<OktaJwtTokenProvider>(),
                JwtProviderType.AzureAd => throw new NotImplementedException("Azure AD JWT provider not yet implemented"),
                JwtProviderType.Custom => throw new NotImplementedException("Custom JWT provider not yet implemented"),
                _ => throw new InvalidOperationException($"Unknown JWT provider type: {options.Value.ProviderType}")
            };
        });

        // Register individual provider implementations
        services.AddSingleton<LocalJwtTokenProvider>();
        services.AddSingleton<OktaJwtTokenProvider>();

        return services;
    }

    /// <summary>
    /// Adds local JWT authentication for development/testing
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="signingKey">JWT signing key</param>
    /// <param name="issuer">Token issuer (default: agent-system)</param>
    /// <param name="audience">Token audience (default: agent-system-api)</param>
    /// <param name="defaultExpiration">Default token expiration (default: 8 hours)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddLocalJwtAuthentication(
        this IServiceCollection services,
        string signingKey,
        string issuer = "agent-system",
        string audience = "agent-system-api",
        TimeSpan? defaultExpiration = null)
    {
        return services.AddJwtAuthentication(
            new ConfigurationBuilder().Build(),
            options =>
            {
                options.ProviderType = JwtProviderType.Local;
                options.Local = new LocalJwtOptions
                {
                    SigningKey = signingKey,
                    Issuer = issuer,
                    Audience = audience,
                    DefaultExpiration = defaultExpiration ?? TimeSpan.FromHours(8)
                };
            });
    }

    /// <summary>
    /// Adds Okta JWT authentication for production
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="domain">Okta domain (e.g., https://yourcompany.okta.com)</param>
    /// <param name="clientId">Okta client ID</param>
    /// <param name="authorizationServerId">Authorization server ID (default: default)</param>
    /// <param name="validAudiences">Valid audiences for token validation</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddOktaJwtAuthentication(
        this IServiceCollection services,
        string domain,
        string clientId,
        string authorizationServerId = "default",
        params string[] validAudiences)
    {
        return services.AddJwtAuthentication(
            new ConfigurationBuilder().Build(),
            options =>
            {
                options.ProviderType = JwtProviderType.Okta;
                options.Okta = new OktaJwtOptions
                {
                    Domain = domain,
                    ClientId = clientId,
                    AuthorizationServerId = authorizationServerId,
                    ValidAudiences = validAudiences.ToList()
                };
            });
    }

    /// <summary>
    /// Adds JWT authentication with secret integration
    /// Gets sensitive values (signing keys, client secrets) from the secret manager
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="configureOptions">Optional configuration options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthenticationWithSecrets(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtAuthenticationOptions>? configureOptions = null)
    {
        // This method requires the secret management system to be already configured
        services.AddJwtAuthentication(configuration, options =>
        {
            // Configuration will be enhanced by secret provider at runtime
            configureOptions?.Invoke(options);
        });

        // Add a configuration source that retrieves secrets for JWT settings
        services.AddSingleton<IJwtSecretsProvider, JwtSecretsProvider>();

        return services;
    }
}

/// <summary>
/// Interface for providing JWT secrets from the secret management system
/// </summary>
public interface IJwtSecretsProvider
{
    /// <summary>
    /// Gets the JWT signing key from secrets
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signing key</returns>
    Task<string> GetSigningKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Okta client secret from secrets
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Client secret</returns>
    Task<string?> GetOktaClientSecretAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Okta API token from secrets
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API token</returns>
    Task<string?> GetOktaApiTokenAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of JWT secrets provider using the secret management system
/// </summary>
internal class JwtSecretsProvider : IJwtSecretsProvider
{
    private readonly ISecretManager _secretManager;
    private readonly ILogger<JwtSecretsProvider> _logger;

    public JwtSecretsProvider(ISecretManager secretManager, ILogger<JwtSecretsProvider> logger)
    {
        _secretManager = secretManager;
        _logger = logger;
    }

    public async Task<string> GetSigningKeyAsync(CancellationToken cancellationToken = default)
    {
        var signingKey = await _secretManager.GetSecretAsync("jwt-signing-key", cancellationToken);
        
        if (string.IsNullOrEmpty(signingKey))
        {
            _logger.LogError("JWT signing key not found in secret manager");
            throw new InvalidOperationException("JWT signing key not configured in secret manager");
        }

        return signingKey;
    }

    public async Task<string?> GetOktaClientSecretAsync(CancellationToken cancellationToken = default)
    {
        var clientSecret = await _secretManager.GetSecretAsync("okta-client-secret", cancellationToken);
        
        if (string.IsNullOrEmpty(clientSecret))
        {
            _logger.LogWarning("Okta client secret not found in secret manager");
        }

        return clientSecret;
    }

    public async Task<string?> GetOktaApiTokenAsync(CancellationToken cancellationToken = default)
    {
        var apiToken = await _secretManager.GetSecretAsync("okta-api-token", cancellationToken);
        
        if (string.IsNullOrEmpty(apiToken))
        {
            _logger.LogWarning("Okta API token not found in secret manager");
        }

        return apiToken;
    }
}