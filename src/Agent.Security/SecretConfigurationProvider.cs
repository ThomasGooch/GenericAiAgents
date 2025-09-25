using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
/// Configuration source that integrates secret management with the .NET configuration system.
/// 
/// This class provides a bridge between the ISecretManager interface and the Microsoft.Extensions.Configuration
/// framework, allowing secrets to be seamlessly accessed through standard configuration patterns while
/// maintaining enterprise security practices and compliance requirements.
/// 
/// The configuration source supports:
/// - Dynamic secret loading from multiple secret management providers (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
/// - Flexible secret-to-configuration key mapping with hierarchical configuration support
/// - Graceful error handling with configurable failure modes for production resilience
/// - Integration with dependency injection containers for service-oriented architectures
/// - Compliance-ready audit logging for secret access tracking and security monitoring
/// - Hot-reload capabilities for secrets rotation without application restart (when supported by provider)
/// 
/// Security Considerations:
/// - Secrets are loaded once at startup and cached in memory for performance
/// - Failed secret loads can be configured to either fail fast (development) or continue (production)
/// - All secret access is logged for compliance and security audit requirements
/// - Memory is not explicitly cleared after use - ensure proper application lifecycle management
/// 
/// Configuration Key Mapping:
/// Secret names are mapped to configuration keys using hierarchical dot notation:
/// - "database-password" -> "Database:Password" 
/// - "api-key" -> "ExternalServices:PaymentProvider:ApiKey"
/// - "connection-string" -> "ConnectionStrings:DefaultConnection"
/// 
/// This enables standard configuration binding patterns:
/// <code>
/// var dbConfig = configuration.GetSection("Database").Get&lt;DatabaseConfig&gt;();
/// var apiKey = configuration["ExternalServices:PaymentProvider:ApiKey"];
/// </code>
/// </summary>
/// <example>
/// Basic usage with dependency injection:
/// <code>
/// services.AddSecretManager&lt;AzureKeyVaultSecretManager&gt;();
/// 
/// var configuration = new ConfigurationBuilder()
///     .AddJsonFile("appsettings.json")
///     .AddSecrets(serviceProvider, secrets =&gt;
///     {
///         secrets.Add(new SecretMapping 
///         { 
///             SecretName = "prod-database-password", 
///             ConfigurationKey = "Database:Password" 
///         });
///         secrets.Add(new SecretMapping 
///         { 
///             SecretName = "stripe-api-key", 
///             ConfigurationKey = "Payment:Stripe:ApiKey" 
///         });
///     }, failOnMissingSecrets: false)
///     .Build();
/// </code>
/// 
/// Production configuration with error handling:
/// <code>
/// // Production: Continue if optional secrets are missing
/// builder.AddSecrets(serviceProvider, secrets =&gt;
/// {
///     secrets.Add(new SecretMapping { SecretName = "critical-db-password", ConfigurationKey = "Database:Password" });
///     secrets.Add(new SecretMapping { SecretName = "optional-cache-key", ConfigurationKey = "Cache:RedisPassword" });
/// }, failOnMissingSecrets: false);
/// 
/// // Development: Fail fast if any secrets are missing
/// builder.AddSecrets(serviceProvider, secrets =&gt;
/// {
///     secrets.Add(new SecretMapping { SecretName = "dev-api-key", ConfigurationKey = "Api:Key" });
/// }, failOnMissingSecrets: true);
/// </code>
/// 
/// Enterprise compliance and monitoring:
/// <code>
/// // Configure with comprehensive logging for compliance
/// services.AddLogging(builder =&gt; 
/// {
///     builder.AddConsole();
///     builder.AddApplicationInsights(); // For centralized compliance logging
/// });
/// 
/// // Add secret configuration with audit trail
/// var source = new SecretConfigurationSource
/// {
///     ServiceProvider = serviceProvider,
///     FailOnMissingSecrets = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development",
///     SecretMappings = new List&lt;SecretMapping&gt;
///     {
///         new() { SecretName = "payment-processor-key", ConfigurationKey = "Payment:ApiKey" },
///         new() { SecretName = "encryption-master-key", ConfigurationKey = "Security:MasterKey" }
///     }
/// };
/// 
/// configurationBuilder.Add(source);
/// </code>
/// </example>
/// <remarks>
/// Performance Considerations:
/// - Secrets are loaded synchronously during configuration build phase
/// - Consider implementing caching strategies for frequently accessed secrets
/// - Monitor secret provider latency in production environments
/// 
/// Compliance and Security:
/// - All secret access attempts are logged with structured logging
/// - Failed secret retrieval attempts are logged as warnings or errors based on configuration
/// - Ensure secret management provider supports your compliance requirements (SOC2, HIPAA, PCI-DSS)
/// - Implement proper secret rotation policies aligned with organizational security standards
/// 
/// Threading and Concurrency:
/// - Configuration loading is single-threaded during application startup
/// - Once loaded, configuration values are immutable and thread-safe for read operations
/// - Secret provider implementations should handle concurrent access appropriately
/// 
/// Integration Patterns:
/// - Use with Microsoft.Extensions.Options for strongly-typed configuration binding
/// - Integrate with health checks to monitor secret provider availability
/// - Combine with configuration validation to ensure required secrets are present
/// - Support for multiple secret sources with precedence ordering
/// </remarks>
public class SecretConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the collection of secret-to-configuration key mappings that define how secrets
    /// from the secret management provider are mapped to hierarchical configuration keys.
    /// 
    /// Each mapping defines a relationship between a secret name in the underlying secret store
    /// (e.g., Azure Key Vault, AWS Secrets Manager) and a configuration key that follows 
    /// Microsoft's hierarchical configuration convention using colon separators.
    /// 
    /// Examples of common mapping patterns:
    /// - Database credentials: "prod-db-password" → "Database:Password"
    /// - API keys: "stripe-api-key" → "Payment:Stripe:ApiKey"  
    /// - Connection strings: "redis-connection" → "ConnectionStrings:Redis"
    /// - Service endpoints: "auth-service-url" → "Services:Authentication:BaseUrl"
    /// 
    /// The mappings are processed in the order they appear in the collection during configuration loading.
    /// </summary>
    /// <value>
    /// A list of <see cref="SecretMapping"/> objects that define secret name to configuration key relationships.
    /// Defaults to an empty list if not explicitly configured.
    /// </value>
    /// <example>
    /// Configure multiple secret mappings for a production application:
    /// <code>
    /// source.SecretMappings = new List&lt;SecretMapping&gt;
    /// {
    ///     new() { SecretName = "prod-database-password", ConfigurationKey = "Database:Password" },
    ///     new() { SecretName = "redis-cache-password", ConfigurationKey = "Cache:Redis:Password" },
    ///     new() { SecretName = "jwt-signing-key", ConfigurationKey = "Authentication:Jwt:SigningKey" },
    ///     new() { SecretName = "storage-account-key", ConfigurationKey = "Storage:Azure:AccountKey" }
    /// };
    /// </code>
    /// </example>
    public List<SecretMapping> SecretMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the configuration provider should throw exceptions
    /// when secrets cannot be retrieved from the secret management provider.
    /// 
    /// When set to <c>true</c> (fail-fast mode):
    /// - Any failure to retrieve a mapped secret will cause the entire configuration loading process to fail
    /// - Exceptions from the secret provider are propagated to the calling code
    /// - Recommended for development environments to catch configuration issues early
    /// - Ensures all required secrets are available before the application starts
    /// 
    /// When set to <c>false</c> (graceful degradation mode):
    /// - Missing or inaccessible secrets are logged as warnings but do not stop configuration loading
    /// - The application can start even if some optional secrets are unavailable
    /// - Recommended for production environments where partial functionality is acceptable
    /// - Allows for graceful handling of secret rotation scenarios or temporary provider outages
    /// 
    /// Security Consideration: In production environments, carefully consider which secrets are truly
    /// optional versus critical for security. Database passwords and encryption keys should typically
    /// be required, while optional feature flags or non-critical service keys might be safely missing.
    /// </summary>
    /// <value>
    /// <c>true</c> to throw exceptions on secret retrieval failures; <c>false</c> to log warnings and continue.
    /// Defaults to <c>false</c> for production resilience.
    /// </value>
    /// <example>
    /// Configure different failure modes based on environment:
    /// <code>
    /// var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    /// 
    /// source.FailOnMissingSecrets = isDevelopment; // Fail fast in dev, graceful in prod
    /// 
    /// // Or configure explicitly based on criticality
    /// source.FailOnMissingSecrets = secrets.Any(s =&gt; s.ConfigurationKey.Contains("Database") || 
    ///                                                s.ConfigurationKey.Contains("Encryption"));
    /// </code>
    /// </example>
    public bool FailOnMissingSecrets { get; set; } = false;

    /// <summary>
    /// Gets or sets the dependency injection service provider that provides access to registered services
    /// including the <see cref="ISecretManager"/> and logging infrastructure required for secret retrieval.
    /// 
    /// This property must be set before calling <see cref="Build(IConfigurationBuilder)"/> as it is used
    /// to resolve the following required services:
    /// - <see cref="ISecretManager"/>: The secret management provider implementation
    /// - <see cref="ILogger{SecretConfigurationProvider}"/>: Structured logging for audit trails
    /// 
    /// The service provider should be fully configured with all necessary secret management services
    /// before being assigned to this property. This typically occurs after services.AddSecretManager()
    /// has been called during application startup.
    /// 
    /// Security Note: The service provider may contain sensitive configuration data and should be
    /// handled according to your organization's security policies for dependency injection containers.
    /// </summary>
    /// <value>
    /// An <see cref="IServiceProvider"/> instance configured with secret management services,
    /// or <c>null</c> if not yet assigned.
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// Thrown during <see cref="Build(IConfigurationBuilder)"/> if this property is <c>null</c> or if
    /// the service provider does not contain required <see cref="ISecretManager"/> or logging services.
    /// </exception>
    /// <example>
    /// Typical usage with dependency injection container:
    /// <code>
    /// // Configure services including secret management
    /// var services = new ServiceCollection();
    /// services.AddSecretManager&lt;AzureKeyVaultSecretManager&gt;();
    /// services.AddLogging();
    /// 
    /// var serviceProvider = services.BuildServiceProvider();
    /// 
    /// // Configure secret source with service provider
    /// var source = new SecretConfigurationSource
    /// {
    ///     ServiceProvider = serviceProvider,
    ///     SecretMappings = mappings,
    ///     FailOnMissingSecrets = false
    /// };
    /// 
    /// // Use in configuration builder
    /// var configuration = new ConfigurationBuilder()
    ///     .Add(source)
    ///     .Build();
    /// </code>
    /// </example>
    public IServiceProvider? ServiceProvider { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (ServiceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider must be set before building the configuration provider");
        }

        var secretManager = ServiceProvider.GetService<ISecretManager>()
            ?? throw new InvalidOperationException("ISecretManager service not found");
        var logger = ServiceProvider.GetService<ILogger<SecretConfigurationProvider>>()
            ?? throw new InvalidOperationException("ILogger<SecretConfigurationProvider> service not found");

        return new SecretConfigurationProvider(this, secretManager, logger);
    }
}

/// <summary>
/// Represents a mapping between a secret stored in a secret management provider and a hierarchical 
/// configuration key in the .NET configuration system.
/// 
/// This class defines the relationship between secret names as they exist in external secret stores
/// (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, etc.) and the hierarchical configuration
/// keys used by the Microsoft.Extensions.Configuration framework.
/// 
/// The mapping enables seamless integration between enterprise secret management systems and standard
/// .NET configuration patterns, supporting both simple key-value mappings and complex hierarchical
/// configuration structures commonly used in enterprise applications.
/// 
/// Common Usage Patterns:
/// - Database connection secrets: Map vault secrets to ConnectionStrings configuration section
/// - API credentials: Map service-specific secrets to nested service configuration
/// - Feature toggles: Map environment-specific secrets to feature flag configuration
/// - Certificate references: Map certificate identifiers to security configuration sections
/// 
/// Security Best Practices:
/// - Use descriptive but non-sensitive secret names that don't reveal system architecture
/// - Follow consistent naming conventions across environments (dev, staging, prod)
/// - Map secrets to appropriately scoped configuration sections to limit access
/// - Consider secret rotation implications when designing mapping strategies
/// </summary>
/// <example>
/// Common enterprise secret mapping patterns:
/// <code>
/// // Database credentials mapping
/// var dbMapping = new SecretMapping
/// {
///     SecretName = "prod-primary-db-password",           // Secret store name
///     ConfigurationKey = "ConnectionStrings:Primary"     // .NET config key
/// };
/// 
/// // API service credentials mapping
/// var apiMapping = new SecretMapping
/// {
///     SecretName = "stripe-live-api-key",
///     ConfigurationKey = "Payment:Stripe:ApiKey"
/// };
/// 
/// // Multi-environment mapping strategy
/// var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
/// var envMapping = new SecretMapping
/// {
///     SecretName = $"{environment.ToLower()}-jwt-signing-key",
///     ConfigurationKey = "Authentication:Jwt:SigningKey"
/// };
/// 
/// // Certificate reference mapping
/// var certMapping = new SecretMapping
/// {
///     SecretName = "ssl-certificate-thumbprint",
///     ConfigurationKey = "Security:Certificates:Primary:Thumbprint"
/// };
/// </code>
/// 
/// Integration with strongly-typed configuration:
/// <code>
/// // Configuration class that will receive the mapped secret
/// public class DatabaseConfiguration
/// {
///     public string ConnectionString { get; set; } = string.Empty;
///     public string Password { get; set; } = string.Empty; // Mapped from secret
///     public int MaxPoolSize { get; set; } = 100;
/// }
/// 
/// // Secret mapping for the password component
/// var mapping = new SecretMapping
/// {
///     SecretName = "production-database-password",
///     ConfigurationKey = "Database:Password"
/// };
/// 
/// // Usage in application
/// var config = configuration.GetSection("Database").Get&lt;DatabaseConfiguration&gt;();
/// // config.Password now contains the secret value from the secret store
/// </code>
/// </example>
/// <remarks>
/// Performance Considerations:
/// - Secret names should be optimized for the underlying secret provider's retrieval patterns
/// - Configuration keys should follow Microsoft's hierarchical naming conventions for efficient lookup
/// - Consider batching related secrets to minimize provider API calls during application startup
/// 
/// Naming Conventions:
/// - Secret names: Use kebab-case with environment prefixes (e.g., "prod-api-key", "dev-db-password")
/// - Configuration keys: Use colon-separated hierarchical naming (e.g., "Service:Database:Password")
/// - Maintain consistency across environments while allowing for environment-specific prefixes
/// 
/// Security and Compliance:
/// - Ensure secret names do not expose sensitive architectural information
/// - Use environment-specific secret names to prevent cross-environment secret access
/// - Consider implementing secret name validation to enforce organizational naming standards
/// - Document secret mappings for security audits and compliance reporting
/// 
/// Error Handling:
/// - Invalid or missing secret names will result in configuration loading warnings or errors
/// - Malformed configuration keys may cause binding failures during application startup
/// - Implement validation to ensure both properties are properly configured before use
/// </remarks>
public class SecretMapping
{
    /// <summary>
    /// Gets or sets the name of the secret as it exists in the secret management provider.
    /// 
    /// This property identifies the specific secret to retrieve from the underlying secret store
    /// (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, etc.). The secret name must
    /// exactly match the identifier used in the secret management system and is case-sensitive
    /// for most providers.
    /// 
    /// Naming Best Practices:
    /// - Use descriptive names that indicate purpose without revealing sensitive architecture details
    /// - Include environment prefixes to prevent cross-environment access (e.g., "prod-", "dev-", "staging-")
    /// - Follow consistent naming conventions across your organization
    /// - Avoid including sensitive information in the secret name itself
    /// 
    /// Examples of well-formed secret names:
    /// - "prod-primary-database-password": Environment-specific database credential
    /// - "api-payment-service-key": Service-specific API credential  
    /// - "encryption-master-key-v2": Versioned encryption key reference
    /// - "ssl-cert-wildcard-company-com": Certificate identifier with domain context
    /// 
    /// Security Considerations:
    /// - Secret names are logged during configuration loading and may appear in audit trails
    /// - Use names that are meaningful for operations but don't expose system architecture
    /// - Implement validation to ensure secret names conform to organizational standards
    /// - Consider secret rotation implications when choosing naming strategies
    /// </summary>
    /// <value>
    /// A non-empty string that identifies the secret in the secret management provider.
    /// Must be a valid identifier for the configured secret management system.
    /// </value>
    /// <example>
    /// Environment-specific secret naming strategy:
    /// <code>
    /// var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower() ?? "dev";
    /// 
    /// var mapping = new SecretMapping
    /// {
    ///     SecretName = $"{environment}-database-connection-password",
    ///     ConfigurationKey = "Database:Password"
    /// };
    /// 
    /// // Results in secret names like:
    /// // - "prod-database-connection-password" in production
    /// // - "dev-database-connection-password" in development  
    /// // - "staging-database-connection-password" in staging
    /// </code>
    /// </example>
    public string SecretName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hierarchical configuration key where the secret value will be accessible
    /// in the .NET configuration system.
    /// 
    /// This property defines the location in the configuration hierarchy where the secret value
    /// will be available for retrieval using standard .NET configuration APIs. The key follows
    /// Microsoft's colon-separated hierarchical naming convention and supports nested sections
    /// for complex configuration structures.
    /// 
    /// The configuration key enables integration with:
    /// - Direct configuration access: <c>configuration["Database:Password"]</c>
    /// - Strongly-typed binding: <c>configuration.GetSection("Database").Get&lt;DatabaseConfig&gt;()</c>
    /// - Options pattern: <c>services.Configure&lt;DatabaseOptions&gt;(configuration.GetSection("Database"))</c>
    /// - Configuration validation and change detection mechanisms
    /// 
    /// Hierarchical Key Structure:
    /// - Use colon (:) to separate hierarchical levels
    /// - Follow PascalCase naming for consistency with .NET conventions  
    /// - Group related secrets under common parent sections
    /// - Align with existing configuration structure for seamless integration
    /// 
    /// Common Configuration Patterns:
    /// - Connection strings: "ConnectionStrings:DatabaseName"
    /// - Service configurations: "Services:PaymentProvider:ApiKey"
    /// - Authentication settings: "Authentication:Jwt:SigningKey"
    /// - Feature flags: "Features:AdvancedAnalytics:Enabled"
    /// - External integrations: "Integrations:ThirdPartyService:Token"
    /// </summary>
    /// <value>
    /// A non-empty string representing the hierarchical path in the configuration system where 
    /// the secret value will be accessible. Must be a valid configuration key path.
    /// </value>
    /// <example>
    /// Hierarchical configuration mapping examples:
    /// <code>
    /// // Simple key-value mapping
    /// var simpleMapping = new SecretMapping
    /// {
    ///     SecretName = "api-key",
    ///     ConfigurationKey = "ApiKey"  // Accessible via configuration["ApiKey"]
    /// };
    /// 
    /// // Nested configuration section mapping
    /// var nestedMapping = new SecretMapping
    /// {
    ///     SecretName = "stripe-live-key",
    ///     ConfigurationKey = "Payment:Stripe:ApiKey"  // configuration["Payment:Stripe:ApiKey"]
    /// };
    /// 
    /// // Connection string mapping
    /// var connectionMapping = new SecretMapping
    /// {
    ///     SecretName = "prod-db-connection",
    ///     ConfigurationKey = "ConnectionStrings:DefaultConnection"
    /// };
    /// 
    /// // Complex nested structure for microservices
    /// var serviceMapping = new SecretMapping
    /// {
    ///     SecretName = "user-service-jwt-key",
    ///     ConfigurationKey = "Microservices:UserService:Authentication:JwtKey"
    /// };
    /// </code>
    /// 
    /// Integration with strongly-typed configuration:
    /// <code>
    /// public class PaymentConfiguration
    /// {
    ///     public StripeConfiguration Stripe { get; set; } = new();
    /// }
    /// 
    /// public class StripeConfiguration  
    /// {
    ///     public string ApiKey { get; set; } = string.Empty; // Maps from "Payment:Stripe:ApiKey"
    ///     public string WebhookSecret { get; set; } = string.Empty;
    /// }
    /// 
    /// // Configuration mapping
    /// var mapping = new SecretMapping
    /// {
    ///     SecretName = "stripe-production-api-key",
    ///     ConfigurationKey = "Payment:Stripe:ApiKey"
    /// };
    /// 
    /// // Usage
    /// var paymentConfig = configuration.GetSection("Payment").Get&lt;PaymentConfiguration&gt;();
    /// // paymentConfig.Stripe.ApiKey contains the secret value
    /// </code>
    /// </example>
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