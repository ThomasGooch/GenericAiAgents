using Agent.Configuration;
using Agent.Configuration.Models;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.IO;
using System.Text.Json;

namespace Agent.Configuration.Tests;

public class ConfigurationProviderTests
{
    private readonly IAgentConfigurationProvider _configProvider;

    public ConfigurationProviderTests()
    {
        _configProvider = new AgentConfigurationProvider();
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithValidJson_ShouldReturnConfiguration()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), "test-config.json");
        var configContent = """
        {
          "AgentSystem": {
            "Name": "TestSystem",
            "Version": "1.0.0",
            "Environment": "Development"
          },
          "Agents": {
            "MaxConcurrentAgents": 10,
            "DefaultTimeout": "00:05:00"
          },
          "Logging": {
            "Level": "Information",
            "Providers": ["Console", "File"]
          }
        }
        """;

        await File.WriteAllTextAsync(configPath, configContent);

        try
        {
            // Act
            var config = await _configProvider.LoadConfigurationAsync(configPath);

            // Assert
            Assert.NotNull(config);
            Assert.Equal("TestSystem", config.AgentSystem.Name);
            Assert.Equal("1.0.0", config.AgentSystem.Version);
            Assert.Equal("Development", config.AgentSystem.Environment);
            Assert.Equal(10, config.Agents.MaxConcurrentAgents);
            Assert.Equal(TimeSpan.FromMinutes(5), config.Agents.DefaultTimeout);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithEnvironmentOverrides_ShouldMergeConfigurations()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AgentSystem__Environment", "Production");
        Environment.SetEnvironmentVariable("Agents__MaxConcurrentAgents", "20");

        var configPath = Path.Combine(Path.GetTempPath(), "test-env-config.json");
        var configContent = """
        {
          "AgentSystem": {
            "Name": "TestSystem",
            "Version": "1.0.0",
            "Environment": "Development"
          },
          "Agents": {
            "MaxConcurrentAgents": 10,
            "DefaultTimeout": "00:05:00"
          }
        }
        """;

        await File.WriteAllTextAsync(configPath, configContent);

        try
        {
            // Act
            var config = await _configProvider.LoadConfigurationAsync(configPath, useEnvironmentVariables: true);

            // Assert
            Assert.Equal("Production", config.AgentSystem.Environment); // Overridden by env var
            Assert.Equal(20, config.Agents.MaxConcurrentAgents); // Overridden by env var
            Assert.Equal("TestSystem", config.AgentSystem.Name); // From file
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
            Environment.SetEnvironmentVariable("AgentSystem__Environment", null);
            Environment.SetEnvironmentVariable("Agents__MaxConcurrentAgents", null);
        }
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithValidConfig_ShouldReturnSuccess()
    {
        // Arrange
        var config = new AgentSystemConfiguration
        {
            AgentSystem = new AgentSystemSettings
            {
                Name = "ValidSystem",
                Version = "1.0.0",
                Environment = "Development"
            },
            Agents = new AgentSettings
            {
                MaxConcurrentAgents = 5,
                DefaultTimeout = TimeSpan.FromMinutes(2)
            }
        };

        // Act
        var result = await _configProvider.ValidateConfigurationAsync(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithInvalidConfig_ShouldReturnErrors()
    {
        // Arrange
        var config = new AgentSystemConfiguration
        {
            AgentSystem = new AgentSystemSettings
            {
                Name = "", // Invalid: empty name
                Version = "invalid-version", // Invalid: non-semantic version
                Environment = "InvalidEnvironment"
            },
            Agents = new AgentSettings
            {
                MaxConcurrentAgents = 0, // Invalid: must be positive
                DefaultTimeout = TimeSpan.FromSeconds(-1) // Invalid: negative timeout
            }
        };

        // Act
        var result = await _configProvider.ValidateConfigurationAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("The Name field is required"));
        Assert.Contains(result.Errors, e => e.Contains("MaxConcurrentAgents must be positive"));
        Assert.Contains(result.Errors, e => e.Contains("DefaultTimeout cannot be negative or zero"));
    }

    [Fact]
    public async Task GetConfigurationAsync_WithCaching_ShouldReturnCachedValue()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), "cached-config.json");
        var configContent = """
        {
          "AgentSystem": {
            "Name": "CachedSystem",
            "Version": "1.0.0",
            "Environment": "Test"
          }
        }
        """;

        await File.WriteAllTextAsync(configPath, configContent);

        try
        {
            // Act - Load twice
            var config1 = await _configProvider.GetConfigurationAsync(configPath);
            var config2 = await _configProvider.GetConfigurationAsync(configPath);

            // Assert
            Assert.NotNull(config1);
            Assert.NotNull(config2);
            Assert.Equal("CachedSystem", config1.AgentSystem.Name);
            Assert.Equal("CachedSystem", config2.AgentSystem.Name);

            // Should be same reference if cached properly
            Assert.Same(config1, config2);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithMissingFile_ShouldThrowException()
    {
        // Arrange
        var nonExistentPath = "/non/existent/config.json";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _configProvider.LoadConfigurationAsync(nonExistentPath));
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithInvalidJson_ShouldThrowException()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), "invalid-config.json");
        var invalidJson = "{ invalid json content }";

        await File.WriteAllTextAsync(configPath, invalidJson);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                _configProvider.LoadConfigurationAsync(configPath));
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    [Fact]
    public void GetEnvironmentConfiguration_ShouldReturnEnvironmentBasedConfig()
    {
        // Arrange
        var environment = "Production";

        // Act
        var config = _configProvider.GetEnvironmentConfiguration(environment);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(environment, config.AgentSystem.Environment);
        Assert.True(config.Agents.MaxConcurrentAgents > 0);
        Assert.True(config.Agents.DefaultTimeout > TimeSpan.Zero);
    }
}