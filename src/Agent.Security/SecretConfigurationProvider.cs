using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Agent.Security;

/// <summary>
/// Configuration provider that retrieves secrets from the secret manager
/// and makes them available to the .NET configuration system
/// </summary>
public class SecretConfigurationProvider : ConfigurationProvider
{
    private readonly ISecretManager _secretManager;
    private readonly ILogger<SecretConfigurationProvider> _logger;
    private readonly SecretConfigurationSource _source;

    public SecretConfigurationProvider(
        SecretConfigurationSource source,
        ISecretManager secretManager,
        ILogger<SecretConfigurationProvider> logger)
    {
        _source = source;
        _secretManager = secretManager;
        _logger = logger;
    }

    public override void Load()
    {
        LoadAsync().GetAwaiter().GetResult();
    }

    private async Task LoadAsync()
    {
        try
        {
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            // Load predefined secrets
            foreach (var secretMapping in _source.SecretMappings)
            {
                try
                {
                    var secretValue = await _secretManager.GetSecretAsync(secretMapping.SecretName);
                    if (secretValue != null)
                    {
                        Data[secretMapping.ConfigurationKey] = secretValue;
                        _logger.LogDebug("Loaded secret '{SecretName}' into configuration key '{ConfigKey}'", 
                            secretMapping.SecretName, secretMapping.ConfigurationKey);
                    }
                    else
                    {
                        _logger.LogWarning("Secret '{SecretName}' not found for configuration key '{ConfigKey}'", 
                            secretMapping.SecretName, secretMapping.ConfigurationKey);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load secret '{SecretName}' for configuration key '{ConfigKey}'", 
                        secretMapping.SecretName, secretMapping.ConfigurationKey);
                    
                    if (_source.FailOnMissingSecrets)
                    {
                        throw;
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} secrets into configuration", Data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load secrets into configuration");
            if (_source.FailOnMissingSecrets)
            {
                throw;
            }
        }
    }
}

/// <summary>
/// Configuration source for secrets
/// </summary>
public class SecretConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Secret mappings (secret name -> configuration key)
    /// </summary>
    public List<SecretMapping> SecretMappings { get; set; } = new();

    /// <summary>
    /// Whether to fail if secrets are missing (default: false)
    /// </summary>
    public bool FailOnMissingSecrets { get; set; } = false;

    /// <summary>
    /// Service provider for dependency injection
    /// </summary>
    public IServiceProvider? ServiceProvider { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (ServiceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider must be set before building the configuration provider");
        }

        var secretManager = (ISecretManager)ServiceProvider.GetService(typeof(ISecretManager))
            ?? throw new InvalidOperationException("ISecretManager service not found");
        var logger = (ILogger<SecretConfigurationProvider>)ServiceProvider.GetService(typeof(ILogger<SecretConfigurationProvider>))
            ?? throw new InvalidOperationException("ILogger<SecretConfigurationProvider> service not found");

        return new SecretConfigurationProvider(this, secretManager, logger);
    }
}

/// <summary>
/// Mapping between a secret name and configuration key
/// </summary>
public class SecretMapping
{
    /// <summary>
    /// Name of the secret in the secret manager
    /// </summary>
    public string SecretName { get; set; } = string.Empty;

    /// <summary>
    /// Configuration key to map the secret to
    /// </summary>
    public string ConfigurationKey { get; set; } = string.Empty;
}

/// <summary>
/// Extension methods for adding secret configuration
/// </summary>
public static class SecretConfigurationExtensions
{
    /// <summary>
    /// Adds secrets to the configuration builder
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <param name="serviceProvider">Service provider with secret manager</param>
    /// <param name="configureSecrets">Action to configure secret mappings</param>
    /// <param name="failOnMissingSecrets">Whether to fail if secrets are missing</param>
    /// <returns>Configuration builder for chaining</returns>
    public static IConfigurationBuilder AddSecrets(
        this IConfigurationBuilder builder,
        IServiceProvider serviceProvider,
        Action<List<SecretMapping>> configureSecrets,
        bool failOnMissingSecrets = false)
    {
        var source = new SecretConfigurationSource
        {
            ServiceProvider = serviceProvider,
            FailOnMissingSecrets = failOnMissingSecrets
        };

        configureSecrets(source.SecretMappings);

        return builder.Add(source);
    }

    /// <summary>
    /// Adds common database secrets to configuration
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <param name="serviceProvider">Service provider with secret manager</param>
    /// <param name="failOnMissingSecrets">Whether to fail if secrets are missing</param>
    /// <returns>Configuration builder for chaining</returns>
    public static IConfigurationBuilder AddDatabaseSecrets(
        this IConfigurationBuilder builder,
        IServiceProvider serviceProvider,
        bool failOnMissingSecrets = false)
    {
        return builder.AddSecrets(serviceProvider, secrets =>
        {
            secrets.Add(new SecretMapping
            {
                SecretName = "postgres-password",
                ConfigurationKey = "Database:Password"
            });
            secrets.Add(new SecretMapping
            {
                SecretName = "redis-password",
                ConfigurationKey = "Redis:Password"
            });
        }, failOnMissingSecrets);
    }
}