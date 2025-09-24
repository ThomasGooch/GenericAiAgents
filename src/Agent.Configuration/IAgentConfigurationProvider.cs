using Agent.Configuration.Models;

namespace Agent.Configuration;

/// <summary>
/// Provides configuration loading, validation, and management capabilities
/// </summary>
public interface IAgentConfigurationProvider
{
    /// <summary>
    /// Loads configuration from the specified file path
    /// </summary>
    /// <param name="configPath">Path to configuration file</param>
    /// <param name="useEnvironmentVariables">Whether to merge environment variables</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Loaded configuration</returns>
    Task<AgentSystemConfiguration> LoadConfigurationAsync(string configPath, bool useEnvironmentVariables = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration with caching support
    /// </summary>
    /// <param name="configPath">Path to configuration file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached or loaded configuration</returns>
    Task<AgentSystemConfiguration> GetConfigurationAsync(string configPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a configuration object
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(AgentSystemConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets environment-specific default configuration
    /// </summary>
    /// <param name="environment">Target environment</param>
    /// <returns>Environment-specific configuration</returns>
    AgentSystemConfiguration GetEnvironmentConfiguration(string environment);

    /// <summary>
    /// Saves configuration to file
    /// </summary>
    /// <param name="configuration">Configuration to save</param>
    /// <param name="configPath">Path to save configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveConfigurationAsync(AgentSystemConfiguration configuration, string configPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges two configuration objects
    /// </summary>
    /// <param name="baseConfig">Base configuration</param>
    /// <param name="overrideConfig">Override configuration</param>
    /// <returns>Merged configuration</returns>
    AgentSystemConfiguration MergeConfigurations(AgentSystemConfiguration baseConfig, AgentSystemConfiguration overrideConfig);

    /// <summary>
    /// Watches for configuration file changes and reloads automatically
    /// </summary>
    /// <param name="configPath">Path to watch</param>
    /// <param name="onChanged">Callback when configuration changes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WatchConfigurationChangesAsync(string configPath, Action<AgentSystemConfiguration> onChanged, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of configuration validation
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Whether the configuration is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Additional validation context
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}