using Agent.Security.Implementations;
using Agent.Security.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Agent.Security;

/// <summary>
/// Extension methods for registering secret management services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds secret management services to the DI container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="configureOptions">Optional configuration options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSecretManagement(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SecretManagerOptions>? configureOptions = null)
    {
        // Configure options
        services.Configure<SecretManagerOptions>(options =>
        {
            configuration.GetSection("SecretManagement").Bind(options);
            configureOptions?.Invoke(options);
        });

        // Add memory cache for caching (if not already registered)
        services.AddMemoryCache();

        // Register the appropriate secret manager based on configuration
        services.AddSingleton<ISecretManager>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecretManagerOptions>>();
            var logger = serviceProvider.GetRequiredService<ILogger<ISecretManager>>();

            ISecretManager secretManager = options.Value.Type switch
            {
                SecretManagerType.Environment => serviceProvider.GetRequiredService<EnvironmentSecretManager>(),
                SecretManagerType.AzureKeyVault => serviceProvider.GetRequiredService<AzureKeyVaultSecretManager>(),
                SecretManagerType.AwsSecretsManager => throw new NotImplementedException("AWS Secrets Manager not yet implemented"),
                SecretManagerType.HashiCorpVault => throw new NotImplementedException("HashiCorp Vault not yet implemented"),
                _ => throw new InvalidOperationException($"Unknown secret manager type: {options.Value.Type}")
            };

            // Wrap with caching if enabled
            if (options.Value.EnableCaching)
            {
                var cache = serviceProvider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                var cachedLogger = serviceProvider.GetRequiredService<ILogger<CachedSecretManager>>();
                secretManager = new CachedSecretManager(secretManager, cache, cachedLogger, options);
            }

            return secretManager;
        });

        // Register individual implementations
        services.AddSingleton<EnvironmentSecretManager>();
        services.AddSingleton<AzureKeyVaultSecretManager>();

        return services;
    }

    /// <summary>
    /// Adds secret management services with environment variable backend (for development)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="environmentPrefix">Prefix for environment variables (default: AGENT_SECRET_)</param>
    /// <param name="enableCaching">Whether to enable caching (default: true)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEnvironmentSecretManagement(
        this IServiceCollection services,
        string environmentPrefix = "AGENT_SECRET_",
        bool enableCaching = true)
    {
        return services.AddSecretManagement(
            new ConfigurationBuilder().Build(),
            options =>
            {
                options.Type = SecretManagerType.Environment;
                options.EnvironmentPrefix = environmentPrefix;
                options.EnableCaching = enableCaching;
            });
    }

    /// <summary>
    /// Adds secret management services with Azure Key Vault backend (for production)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="vaultUrl">Azure Key Vault URL</param>
    /// <param name="useManagedIdentity">Whether to use managed identity (default: true)</param>
    /// <param name="enableCaching">Whether to enable caching (default: true)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAzureKeyVaultSecretManagement(
        this IServiceCollection services,
        string vaultUrl,
        bool useManagedIdentity = true,
        bool enableCaching = true)
    {
        return services.AddSecretManagement(
            new ConfigurationBuilder().Build(),
            options =>
            {
                options.Type = SecretManagerType.AzureKeyVault;
                options.EnableCaching = enableCaching;
                options.AzureKeyVault = new AzureKeyVaultOptions
                {
                    VaultUrl = vaultUrl,
                    UseManagedIdentity = useManagedIdentity
                };
            });
    }
}