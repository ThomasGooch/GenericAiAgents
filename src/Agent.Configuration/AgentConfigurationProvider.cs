using Agent.Configuration.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Agent.Configuration;

/// <summary>
/// Default implementation of configuration provider
/// </summary>
public class AgentConfigurationProvider : IAgentConfigurationProvider
{
    private readonly ConcurrentDictionary<string, AgentSystemConfiguration> _configurationCache = new();
    private readonly IConfigurationValidator _validator;

    public AgentConfigurationProvider()
    {
        _validator = new ConfigurationValidator();
    }

    public async Task<AgentSystemConfiguration> LoadConfigurationAsync(string configPath, bool useEnvironmentVariables = true, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var builder = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false);

        if (useEnvironmentVariables)
        {
            builder.AddEnvironmentVariables();
        }

        var configuration = builder.Build();
        var agentConfig = new AgentSystemConfiguration();

        try
        {
            configuration.Bind(agentConfig);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to bind configuration from {configPath}: {ex.Message}", ex);
        }

        // Validate the loaded configuration
        var validationResult = await ValidateConfigurationAsync(agentConfig, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors);
            throw new InvalidOperationException($"Configuration validation failed: {errors}");
        }

        return agentConfig;
    }

    public async Task<AgentSystemConfiguration> GetConfigurationAsync(string configPath, CancellationToken cancellationToken = default)
    {
        if (_configurationCache.TryGetValue(configPath, out var cachedConfig))
        {
            return cachedConfig;
        }

        var config = await LoadConfigurationAsync(configPath, true, cancellationToken);
        _configurationCache[configPath] = config;
        return config;
    }

    public Task<ConfigurationValidationResult> ValidateConfigurationAsync(AgentSystemConfiguration configuration, CancellationToken cancellationToken = default)
    {
        return _validator.ValidateAsync(configuration);
    }

    public AgentSystemConfiguration GetEnvironmentConfiguration(string environment)
    {
        return environment.ToLowerInvariant() switch
        {
            "development" => new AgentSystemConfiguration
            {
                AgentSystem = new AgentSystemSettings
                {
                    Name = "Agent System",
                    Version = "1.0.0-dev",
                    Environment = "Development"
                },
                Agents = new AgentSettings
                {
                    MaxConcurrentAgents = 5,
                    DefaultTimeout = TimeSpan.FromMinutes(2),
                    HealthCheckInterval = TimeSpan.FromSeconds(30)
                },
                Logging = new LoggingSettings
                {
                    Level = "Debug",
                    Providers = ["Console", "File"]
                }
            },
            "staging" => new AgentSystemConfiguration
            {
                AgentSystem = new AgentSystemSettings
                {
                    Name = "Agent System",
                    Version = "1.0.0-staging",
                    Environment = "Staging"
                },
                Agents = new AgentSettings
                {
                    MaxConcurrentAgents = 10,
                    DefaultTimeout = TimeSpan.FromMinutes(5),
                    HealthCheckInterval = TimeSpan.FromSeconds(30)
                },
                Logging = new LoggingSettings
                {
                    Level = "Information",
                    Providers = ["Console", "File"]
                }
            },
            "production" => new AgentSystemConfiguration
            {
                AgentSystem = new AgentSystemSettings
                {
                    Name = "Agent System",
                    Version = "1.0.0",
                    Environment = "Production"
                },
                Agents = new AgentSettings
                {
                    MaxConcurrentAgents = 20,
                    DefaultTimeout = TimeSpan.FromMinutes(10),
                    HealthCheckInterval = TimeSpan.FromMinutes(1)
                },
                Logging = new LoggingSettings
                {
                    Level = "Warning",
                    Providers = ["Console", "File"]
                }
            },
            _ => new AgentSystemConfiguration
            {
                AgentSystem = new AgentSystemSettings
                {
                    Name = "Agent System",
                    Version = "1.0.0",
                    Environment = environment
                },
                Agents = new AgentSettings
                {
                    MaxConcurrentAgents = 10,
                    DefaultTimeout = TimeSpan.FromMinutes(5),
                    HealthCheckInterval = TimeSpan.FromSeconds(30)
                },
                Logging = new LoggingSettings
                {
                    Level = "Information",
                    Providers = ["Console"]
                }
            }
        };
    }

    public async Task SaveConfigurationAsync(AgentSystemConfiguration configuration, string configPath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(configuration, options);
        await File.WriteAllTextAsync(configPath, json, cancellationToken);

        // Update cache
        _configurationCache[configPath] = configuration;
    }

    public AgentSystemConfiguration MergeConfigurations(AgentSystemConfiguration baseConfig, AgentSystemConfiguration overrideConfig)
    {
        var merged = new AgentSystemConfiguration();

        // Merge system settings
        merged.AgentSystem.Name = !string.IsNullOrEmpty(overrideConfig.AgentSystem.Name)
            ? overrideConfig.AgentSystem.Name
            : baseConfig.AgentSystem.Name;
        merged.AgentSystem.Version = !string.IsNullOrEmpty(overrideConfig.AgentSystem.Version)
            ? overrideConfig.AgentSystem.Version
            : baseConfig.AgentSystem.Version;
        merged.AgentSystem.Environment = !string.IsNullOrEmpty(overrideConfig.AgentSystem.Environment)
            ? overrideConfig.AgentSystem.Environment
            : baseConfig.AgentSystem.Environment;

        // Merge agent settings
        merged.Agents.MaxConcurrentAgents = overrideConfig.Agents.MaxConcurrentAgents != 0
            ? overrideConfig.Agents.MaxConcurrentAgents
            : baseConfig.Agents.MaxConcurrentAgents;
        merged.Agents.DefaultTimeout = overrideConfig.Agents.DefaultTimeout != default
            ? overrideConfig.Agents.DefaultTimeout
            : baseConfig.Agents.DefaultTimeout;

        // Merge other sections as needed
        merged.Database = overrideConfig.Database ?? baseConfig.Database;
        merged.Logging = overrideConfig.Logging ?? baseConfig.Logging;
        merged.Monitoring = overrideConfig.Monitoring ?? baseConfig.Monitoring;
        merged.Security = overrideConfig.Security ?? baseConfig.Security;

        return merged;
    }

    public async Task WatchConfigurationChangesAsync(string configPath, Action<AgentSystemConfiguration> onChanged, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(configPath) ?? ".";
        var fileName = Path.GetFileName(configPath);

        using var watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        var lastWriteTime = DateTime.MinValue;

        watcher.Changed += async (sender, e) =>
        {
            // Debounce rapid file system events
            var currentWriteTime = File.GetLastWriteTime(configPath);
            if (currentWriteTime <= lastWriteTime.AddSeconds(1))
                return;

            lastWriteTime = currentWriteTime;

            try
            {
                await Task.Delay(100, cancellationToken); // Brief delay to ensure file write is complete
                var newConfig = await LoadConfigurationAsync(configPath, true, cancellationToken);

                // Clear cache and update
                _configurationCache.TryRemove(configPath, out _);
                _configurationCache[configPath] = newConfig;

                onChanged(newConfig);
            }
            catch (Exception)
            {
                // Log error in real implementation
                // For now, silently ignore to prevent callback exceptions
            }
        };

        // Keep watching until cancellation is requested
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}