namespace Agent.Security;

/// <summary>
/// Defines the contract for secure secret management operations that abstract various backend storage systems
/// including cloud services, environment variables, and enterprise key management systems. Provides standardized
/// access to sensitive configuration data with built-in security, caching, and compliance features.
/// </summary>
/// <remarks>
/// <para>
/// The ISecretManager interface abstracts secret storage and retrieval operations across different backend systems,
/// enabling applications to securely manage sensitive data such as API keys, connection strings, certificates,
/// and other confidential information without coupling to specific secret management technologies.
/// </para>
/// <para>
/// **Supported Backend Systems:**
/// - **Azure Key Vault**: Enterprise-grade cloud secret management with HSM support
/// - **AWS Secrets Manager**: Amazon's managed secret storage with automatic rotation
/// - **Environment Variables**: Local development and container-based secret injection
/// - **HashiCorp Vault**: Enterprise secret management with dynamic secrets
/// - **Kubernetes Secrets**: Container orchestration platform secret management
/// - **Local Development**: File-based or in-memory storage for development scenarios
/// </para>
/// <para>
/// **Security Features:**
/// - **Encryption at Rest**: All secrets encrypted using industry-standard algorithms
/// - **Encryption in Transit**: Secure communication channels (TLS/HTTPS)
/// - **Access Control**: Role-based and policy-based access management
/// - **Audit Logging**: Comprehensive access and modification tracking
/// - **Secret Rotation**: Support for automatic and manual secret rotation
/// - **Versioning**: Historical secret versions for rollback scenarios
/// </para>
/// <para>
/// **Performance and Caching:**
/// Implementations may include intelligent caching with configurable TTL, automatic refresh,
/// and memory-safe storage to balance performance with security requirements. Caching
/// strategies should consider secret sensitivity and compliance requirements.
/// </para>
/// <para>
/// **Compliance and Governance:**
/// The interface supports enterprise compliance requirements including SOC 2, ISO 27001,
/// GDPR, HIPAA, and PCI DSS through proper access controls, audit logging, and data
/// protection mechanisms provided by underlying secret management systems.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Dependency injection setup
/// services.AddSingleton&lt;ISecretManager, AzureKeyVaultSecretManager&gt;();
/// services.Configure&lt;SecretManagerOptions&gt;(options =&gt;
/// {
///     options.KeyVaultUri = "https://your-vault.vault.azure.net/";
///     options.CachingEnabled = true;
///     options.CacheTtl = TimeSpan.FromMinutes(15);
/// });
/// 
/// // Basic secret operations
/// public class DatabaseService
/// {
///     private readonly ISecretManager _secretManager;
///     
///     public DatabaseService(ISecretManager secretManager)
///     {
///         _secretManager = secretManager;
///     }
///     
///     public async Task&lt;IDbConnection&gt; CreateConnectionAsync()
///     {
///         var connectionString = await _secretManager.GetSecretAsync("DatabaseConnectionString");
///         if (connectionString == null)
///         {
///             throw new InvalidOperationException("Database connection string not found");
///         }
///         
///         return new SqlConnection(connectionString);
///     }
/// }
/// 
/// // AI service configuration with secrets
/// public class AIServiceConfiguration
/// {
///     private readonly ISecretManager _secretManager;
///     
///     public AIServiceConfiguration(ISecretManager secretManager)
///     {
///         _secretManager = secretManager;
///     }
///     
///     public async Task&lt;AIConfiguration&gt; GetAIConfigurationAsync()
///     {
///         var apiKey = await _secretManager.GetSecretAsync("OpenAI-ApiKey");
///         var organizationId = await _secretManager.GetSecretAsync("OpenAI-OrganizationId");
///         
///         return new AIConfiguration
///         {
///             Provider = AIProvider.OpenAI,
///             ApiKey = apiKey,
///             OrganizationId = organizationId,
///             Model = "gpt-4-turbo-preview"
///         };
///     }
/// }
/// 
/// // Batch secret operations for initialization
/// public class ApplicationInitializer
/// {
///     private readonly ISecretManager _secretManager;
///     
///     public ApplicationInitializer(ISecretManager secretManager)
///     {
///         _secretManager = secretManager;
///     }
///     
///     public async Task InitializeAsync()
///     {
///         // Check for required secrets
///         var requiredSecrets = new[]
///         {
///             "DatabaseConnectionString",
///             "OpenAI-ApiKey",
///             "JWT-SigningKey",
///             "Redis-ConnectionString"
///         };
///         
///         var missingSecrets = new List&lt;string&gt;();
///         
///         foreach (var secretName in requiredSecrets)
///         {
///             if (!await _secretManager.SecretExistsAsync(secretName))
///             {
///                 missingSecrets.Add(secretName);
///             }
///         }
///         
///         if (missingSecrets.Any())
///         {
///             throw new InvalidOperationException(
///                 $"Missing required secrets: {string.Join(", ", missingSecrets)}");
///         }
///         
///         // Initialize application components with secrets
///         await InitializeDatabaseAsync();
///         await InitializeAIServicesAsync();
///         await InitializeCachingAsync();
///     }
/// }
/// 
/// // Secret management operations
/// public class SecretManagementService
/// {
///     private readonly ISecretManager _secretManager;
///     private readonly ILogger&lt;SecretManagementService&gt; _logger;
///     
///     public SecretManagementService(ISecretManager secretManager, ILogger&lt;SecretManagementService&gt; logger)
///     {
///         _secretManager = secretManager;
///         _logger = logger;
///     }
///     
///     public async Task RotateApiKeyAsync(string serviceName)
///     {
///         var secretName = $"{serviceName}-ApiKey";
///         var backupSecretName = $"{serviceName}-ApiKey-Previous";
///         
///         try
///         {
///             // Backup current secret
///             var currentSecret = await _secretManager.GetSecretAsync(secretName);
///             if (currentSecret != null)
///             {
///                 await _secretManager.SetSecretAsync(backupSecretName, currentSecret);
///             }
///             
///             // Generate and store new secret
///             var newApiKey = GenerateNewApiKey();
///             await _secretManager.SetSecretAsync(secretName, newApiKey);
///             
///             _logger.LogInformation("Successfully rotated API key for service: {ServiceName}", serviceName);
///         }
///         catch (Exception ex)
///         {
///             _logger.LogError(ex, "Failed to rotate API key for service: {ServiceName}", serviceName);
///             throw;
///         }
///     }
///     
///     public async Task&lt;Dictionary&lt;string, bool&gt;&gt; HealthCheckSecretsAsync()
///     {
///         var secretNames = await _secretManager.ListSecretNamesAsync();
///         var results = new Dictionary&lt;string, bool&gt;();
///         
///         foreach (var secretName in secretNames)
///         {
///             try
///             {
///                 var secret = await _secretManager.GetSecretAsync(secretName);
///                 results[secretName] = !string.IsNullOrEmpty(secret);
///             }
///             catch (Exception ex)
///             {
///                 _logger.LogWarning(ex, "Failed to retrieve secret: {SecretName}", secretName);
///                 results[secretName] = false;
///             }
///         }
///         
///         return results;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="AzureKeyVaultSecretManager"/>
/// <seealso cref="EnvironmentSecretManager"/>
/// <seealso cref="CachedSecretManager"/>
/// <seealso cref="SecretManagerOptions"/>
public interface ISecretManager
{
    /// <summary>
    /// Retrieves a secret value by name from the configured secret management backend.
    /// Supports caching, automatic decryption, and version resolution based on implementation.
    /// </summary>
    /// <param name="secretName">
    /// The unique identifier or name of the secret to retrieve. Should follow the naming
    /// conventions of the backend system (e.g., "MyApp/Database/ConnectionString" for hierarchical systems).
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the secret retrieval operation.
    /// Respects both the provided token and any configured operation timeouts.
    /// </param>
    /// <returns>
    /// A task that resolves to the secret value as a string, or null if the secret does not exist.
    /// The value is decrypted and ready for use by the application.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretName"/> is null, empty, or contains invalid characters.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the current security context lacks permission to access the specified secret.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the secret manager has not been properly configured or initialized.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when the operation times out due to network issues or backend unavailability.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method implements intelligent caching when supported by the backend, reducing
    /// latency and API calls while maintaining security boundaries. Cache behavior can be
    /// configured through implementation-specific options.
    /// </para>
    /// <para>
    /// **Security Considerations:**
    /// - Returned values should be treated as sensitive and not logged or exposed
    /// - Memory containing secret values should be cleared when no longer needed
    /// - Consider using SecureString for highly sensitive scenarios
    /// </para>
    /// <para>
    /// **Performance Notes:**
    /// - First access may be slower due to authentication and network calls
    /// - Subsequent accesses may benefit from caching if enabled
    /// - Consider batch operations for multiple secret retrievals
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic secret retrieval with error handling
    /// try
    /// {
    ///     var connectionString = await secretManager.GetSecretAsync("DatabaseConnectionString");
    ///     if (connectionString != null)
    ///     {
    ///         // Use the connection string
    ///         var connection = new SqlConnection(connectionString);
    ///     }
    ///     else
    ///     {
    ///         throw new InvalidOperationException("Database connection string not configured");
    ///     }
    /// }
    /// catch (UnauthorizedAccessException)
    /// {
    ///     logger.LogError("Access denied when retrieving database connection string");
    ///     throw;
    /// }
    /// 
    /// // Retrieving API keys for external services
    /// var apiKey = await secretManager.GetSecretAsync("ExternalAPI/ServiceKey");
    /// var httpClient = new HttpClient();
    /// httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    /// </code>
    /// </example>
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores or updates a secret value in the configured secret management backend.
    /// Supports versioning, encryption, and access control based on implementation capabilities.
    /// </summary>
    /// <param name="secretName">
    /// The unique identifier or name for the secret. Should follow backend naming conventions
    /// and organizational policies for secret naming standards.
    /// </param>
    /// <param name="secretValue">
    /// The sensitive value to store. Will be encrypted automatically by the backend system
    /// using appropriate encryption standards and key management practices.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the secret storage operation.
    /// </param>
    /// <returns>
    /// A task that completes when the secret has been successfully stored and replicated
    /// according to the backend's durability guarantees.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretName"/> is invalid or <paramref name="secretValue"/> exceeds size limits.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the current security context lacks permission to create or modify secrets.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the secret manager is not properly configured or the backend is unavailable.
    /// </exception>
    /// <exception cref="QuotaExceededException">
    /// Thrown when storing the secret would exceed storage quotas or limits.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// **Security Best Practices:**
    /// - Secret values are automatically encrypted at rest using backend encryption
    /// - Previous versions may be retained for rollback scenarios (implementation-dependent)
    /// - Audit trails are automatically created for compliance and security monitoring
    /// - Access control policies are enforced during storage operations
    /// </para>
    /// <para>
    /// **Versioning and History:**
    /// Most enterprise backends maintain secret versions, allowing for rollback scenarios
    /// and audit trails. Check implementation documentation for version retention policies.
    /// </para>
    /// <para>
    /// **Naming Conventions:**
    /// - Use consistent, hierarchical naming: "Application/Component/SecretType"
    /// - Avoid special characters that may conflict with backend systems
    /// - Include environment indicators when appropriate: "MyApp/Prod/DatabaseKey"
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Storing a new API key with proper error handling
    /// try
    /// {
    ///     await secretManager.SetSecretAsync("OpenAI/ProductionKey", generatedApiKey);
    ///     logger.LogInformation("Successfully stored OpenAI API key");
    /// }
    /// catch (UnauthorizedAccessException)
    /// {
    ///     logger.LogError("Insufficient permissions to store API key");
    ///     throw;
    /// }
    /// catch (QuotaExceededException)
    /// {
    ///     logger.LogError("Storage quota exceeded - unable to store new secret");
    ///     throw;
    /// }
    /// 
    /// // Rotating a secret (store new, keep old as backup)
    /// var oldSecret = await secretManager.GetSecretAsync("ServiceKey");
    /// if (oldSecret != null)
    /// {
    ///     await secretManager.SetSecretAsync("ServiceKey-Backup", oldSecret);
    /// }
    /// await secretManager.SetSecretAsync("ServiceKey", newSecretValue);
    /// </code>
    /// </example>
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a secret from the secret management backend.
    /// This operation may support soft-delete with recovery periods depending on implementation.
    /// </summary>
    /// <param name="secretName">
    /// The unique identifier or name of the secret to delete. Must exactly match an existing secret name.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the delete operation.
    /// </param>
    /// <returns>
    /// A task that completes when the secret has been successfully deleted from the backend system.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretName"/> is null, empty, or contains invalid characters.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the current security context lacks permission to delete the specified secret.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified secret does not exist in the backend system.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the secret manager is not properly configured or the delete operation fails.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// **Deletion Behavior:**
    /// - Some backends implement soft-delete with recovery periods (e.g., Azure Key Vault)
    /// - Others perform immediate permanent deletion (e.g., environment variables)
    /// - Check implementation documentation for specific deletion semantics
    /// </para>
    /// <para>
    /// **Security and Compliance:**
    /// - Deletion operations are logged for audit compliance
    /// - Some systems may require additional permissions for delete operations
    /// - Consider backup procedures before deleting critical secrets
    /// </para>
    /// <para>
    /// **Recovery Considerations:**
    /// - Plan for secret recovery scenarios in production environments
    /// - Document secret recreation procedures for business continuity
    /// - Consider using secret rotation instead of deletion for active secrets
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Safe secret deletion with existence check
    /// var secretName = "TemporaryApiKey";
    /// 
    /// if (await secretManager.SecretExistsAsync(secretName))
    /// {
    ///     try
    ///     {
    ///         await secretManager.DeleteSecretAsync(secretName);
    ///         logger.LogInformation("Successfully deleted secret: {SecretName}", secretName);
    ///     }
    ///     catch (UnauthorizedAccessException)
    ///     {
    ///         logger.LogError("Insufficient permissions to delete secret: {SecretName}", secretName);
    ///         throw;
    ///     }
    /// }
    /// else
    /// {
    ///     logger.LogWarning("Secret {SecretName} does not exist, skipping deletion", secretName);
    /// }
    /// 
    /// // Bulk cleanup of test secrets
    /// var testSecrets = await secretManager.ListSecretNamesAsync();
    /// var testSecretNames = testSecrets.Where(name =&gt; name.StartsWith("Test/")).ToList();
    /// 
    /// foreach (var testSecretName in testSecretNames)
    /// {
    ///     try
    ///     {
    ///         await secretManager.DeleteSecretAsync(testSecretName);
    ///         logger.LogInformation("Deleted test secret: {SecretName}", testSecretName);
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         logger.LogWarning(ex, "Failed to delete test secret: {SecretName}", testSecretName);
    ///     }
    /// }
    /// </code>
    /// </example>
    Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a collection of all available secret names from the configured backend system.
    /// Useful for inventory management, validation, and administrative operations.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the list operation.
    /// </param>
    /// <returns>
    /// A task that resolves to an enumerable collection of secret names accessible to the current security context.
    /// Names are returned as they exist in the backend system without values.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the current security context lacks permission to list secrets.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the secret manager is not properly configured or the backend is unavailable.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// **Security Considerations:**
    /// - Only secret names are returned, not values
    /// - Results are filtered based on current user's access permissions
    /// - Some backends may limit listing capabilities for security reasons
    /// </para>
    /// <para>
    /// **Performance Notes:**
    /// - This operation may be expensive for backends with large numbers of secrets
    /// - Consider caching results if used frequently
    /// - Some implementations may support filtering or pagination
    /// </para>
    /// <para>
    /// **Use Cases:**
    /// - Application startup validation of required secrets
    /// - Administrative inventory and cleanup operations
    /// - Migration and backup scenarios
    /// - Monitoring and alerting for missing secrets
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Validate required secrets exist at startup
    /// var requiredSecrets = new[] 
    /// {
    ///     "Database/ConnectionString",
    ///     "OpenAI/ApiKey", 
    ///     "JWT/SigningKey"
    /// };
    /// 
    /// var availableSecrets = await secretManager.ListSecretNamesAsync();
    /// var availableSecretSet = new HashSet&lt;string&gt;(availableSecrets);
    /// 
    /// var missingSecrets = requiredSecrets.Where(name =&gt; !availableSecretSet.Contains(name)).ToList();
    /// 
    /// if (missingSecrets.Any())
    /// {
    ///     throw new InvalidOperationException($"Missing required secrets: {string.Join(", ", missingSecrets)}");
    /// }
    /// 
    /// // Generate secret inventory report
    /// var allSecrets = await secretManager.ListSecretNamesAsync();
    /// var secretsByPrefix = allSecrets.GroupBy(name =&gt; name.Split('/').FirstOrDefault() ?? "Root")
    ///                                 .ToDictionary(g =&gt; g.Key, g =&gt; g.Count());
    /// 
    /// foreach (var category in secretsByPrefix)
    /// {
    ///     logger.LogInformation("Secret category {Category}: {Count} secrets", 
    ///                          category.Key, category.Value);
    /// }
    /// </code>
    /// </example>
    Task<IEnumerable<string>> ListSecretNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a specific secret exists in the configured backend system without retrieving its value.
    /// Provides efficient existence validation for conditional operations and error handling.
    /// </summary>
    /// <param name="secretName">
    /// The unique identifier or name of the secret to check for existence.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the existence check operation.
    /// </param>
    /// <returns>
    /// A task that resolves to true if the secret exists and is accessible to the current security context,
    /// false otherwise.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretName"/> is null, empty, or contains invalid characters.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the secret manager is not properly configured or the backend is unavailable.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// **Performance Benefits:**
    /// - More efficient than calling GetSecretAsync and checking for null
    /// - Avoids unnecessary value retrieval and decryption operations
    /// - Reduces network traffic and backend resource usage
    /// </para>
    /// <para>
    /// **Security Considerations:**
    /// - Returns false if secret exists but is not accessible to current context
    /// - Does not distinguish between "does not exist" and "access denied"
    /// - Useful for defensive programming without exposing sensitive information
    /// </para>
    /// <para>
    /// **Common Use Cases:**
    /// - Conditional configuration based on secret availability
    /// - Validation before attempting secret operations
    /// - Health checks and monitoring scenarios
    /// - Migration and synchronization processes
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Conditional configuration based on secret existence
    /// var useAdvancedFeatures = await secretManager.SecretExistsAsync("AdvancedFeatures/License");
    /// 
    /// if (useAdvancedFeatures)
    /// {
    ///     var license = await secretManager.GetSecretAsync("AdvancedFeatures/License");
    ///     await EnableAdvancedFeaturesAsync(license);
    /// }
    /// else
    /// {
    ///     logger.LogInformation("Advanced features not enabled - license not found");
    /// }
    /// 
    /// // Validation before secret rotation
    /// var secretName = "ServiceApiKey";
    /// 
    /// if (await secretManager.SecretExistsAsync(secretName))
    /// {
    ///     // Backup existing secret before rotation
    ///     var currentValue = await secretManager.GetSecretAsync(secretName);
    ///     await secretManager.SetSecretAsync($"{secretName}-Backup", currentValue);
    ///     
    ///     // Set new secret value
    ///     await secretManager.SetSecretAsync(secretName, newSecretValue);
    /// }
    /// else
    /// {
    ///     // Initial secret creation
    ///     await secretManager.SetSecretAsync(secretName, initialSecretValue);
    /// }
    /// 
    /// // Health check implementation
    /// public async Task&lt;HealthCheckResult&gt; CheckSecretsHealthAsync()
    /// {
    ///     var criticalSecrets = new[] { "Database/Connection", "JWT/SigningKey" };
    ///     var missingSecrets = new List&lt;string&gt;();
    ///     
    ///     foreach (var secretName in criticalSecrets)
    ///     {
    ///         if (!await secretManager.SecretExistsAsync(secretName))
    ///         {
    ///             missingSecrets.Add(secretName);
    ///         }
    ///     }
    ///     
    ///     return missingSecrets.Any() 
    ///         ? HealthCheckResult.Unhealthy($"Missing secrets: {string.Join(", ", missingSecrets)}")
    ///         : HealthCheckResult.Healthy();
    /// }
    /// </code>
    /// </example>
    Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default);
}