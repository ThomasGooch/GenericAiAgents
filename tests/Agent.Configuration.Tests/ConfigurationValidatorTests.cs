using Agent.Configuration;
using Agent.Configuration.Models;

namespace Agent.Configuration.Tests;

public class ConfigurationValidatorTests
{
    private readonly IConfigurationValidator _validator;

    public ConfigurationValidatorTests()
    {
        _validator = new ConfigurationValidator();
    }

    [Fact]
    public async Task ValidateAsync_WithCompleteValidConfig_ShouldPass()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ValidateAsync_WithMissingRequiredFields_ShouldFail()
    {
        // Arrange
        var config = new AgentSystemConfiguration
        {
            AgentSystem = new AgentSystemSettings
            {
                Name = "", // Required field missing
                Version = "", // Required field missing
                Environment = null! // Required field missing
            }
        };

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("AgentSystem.Name") && e.Contains("required"));
        Assert.Contains(result.Errors, e => e.Contains("AgentSystem.Version") && e.Contains("required"));
        Assert.Contains(result.Errors, e => e.Contains("AgentSystem.Environment") && e.Contains("required"));
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidValues_ShouldFail()
    {
        // Arrange
        var config = new AgentSystemConfiguration
        {
            AgentSystem = new AgentSystemSettings
            {
                Name = "Valid System",
                Version = "invalid-version-format",
                Environment = "Development"
            },
            Agents = new AgentSettings
            {
                MaxConcurrentAgents = -1, // Invalid: negative
                DefaultTimeout = TimeSpan.FromMilliseconds(-100), // Invalid: negative
                HealthCheckInterval = TimeSpan.FromSeconds(5) // Invalid: too frequent
            },
            Database = new DatabaseSettings
            {
                ConnectionString = "", // Invalid: empty
                MaxConnections = 0, // Invalid: zero
                ConnectionTimeout = TimeSpan.FromSeconds(130) // Warning: high timeout
            }
        };

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("AgentSystem.Version"));
        Assert.Contains(result.Errors, e => e.Contains("Agents.MaxConcurrentAgents"));
        Assert.Contains(result.Errors, e => e.Contains("Agents.DefaultTimeout"));
        Assert.Contains(result.Errors, e => e.Contains("Database.ConnectionString"));
        Assert.Contains(result.Errors, e => e.Contains("Database.MaxConnections"));
        Assert.Contains(result.Warnings, w => w.Contains("Database.ConnectionTimeout"));
    }

    [Fact]
    public async Task ValidateAsync_WithBoundaryValues_ShouldHandleCorrectly()
    {
        // Arrange
        var config = new AgentSystemConfiguration
        {
            AgentSystem = new AgentSystemSettings
            {
                Name = "Test",
                Version = "1.0.0",
                Environment = "Test"
            },
            Agents = new AgentSettings
            {
                MaxConcurrentAgents = 1, // Minimum valid value
                DefaultTimeout = TimeSpan.FromSeconds(1), // Minimum valid value
                HealthCheckInterval = TimeSpan.FromSeconds(10) // Minimum valid value
            },
            Database = new DatabaseSettings
            {
                ConnectionString = "Server=localhost;Database=test;",
                MaxConnections = 1, // Minimum valid value
                ConnectionTimeout = TimeSpan.FromSeconds(1) // Minimum valid value
            }
        };

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithCustomRules_ShouldApplyRules()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var customRule = new ConfigurationValidationRule
        {
            Field = "AgentSystem.Name",
            Rule = (value) => !value.ToString()!.Contains("Test"),
            ErrorMessage = "Name cannot contain 'Test' in production"
        };

        config.AgentSystem.Name = "Test System";

        // Act
        var result = await _validator.ValidateAsync(config, new[] { customRule });

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Name cannot contain 'Test' in production"));
    }

    [Fact]
    public async Task ValidateEnvironmentSpecificAsync_ShouldValidateForEnvironment()
    {
        // Arrange
        var config = new AgentSystemConfiguration
        {
            AgentSystem = new AgentSystemSettings
            {
                Name = "Production System",
                Version = "1.0.0",
                Environment = "Production"
            },
            Agents = new AgentSettings
            {
                MaxConcurrentAgents = 2, // Too low for production
                DefaultTimeout = TimeSpan.FromSeconds(5) // Too short for production
            },
            Logging = new LoggingSettings
            {
                Level = "Debug" // Too verbose for production
            }
        };

        // Act
        var result = await _validator.ValidateEnvironmentSpecificAsync(config, "Production");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("production") && e.Contains("MaxConcurrentAgents"));
        Assert.Contains(result.Warnings, w => w.Contains("Debug") && w.Contains("production"));
    }

    private static AgentSystemConfiguration CreateValidConfiguration()
    {
        return new AgentSystemConfiguration
        {
            AgentSystem = new AgentSystemSettings
            {
                Name = "Valid Agent System",
                Version = "1.0.0",
                Environment = "Development"
            },
            Agents = new AgentSettings
            {
                MaxConcurrentAgents = 5,
                DefaultTimeout = TimeSpan.FromMinutes(2),
                HealthCheckInterval = TimeSpan.FromSeconds(30)
            },
            Database = new DatabaseSettings
            {
                ConnectionString = "Server=localhost;Database=AgentSystem;Trusted_Connection=true;",
                MaxConnections = 20,
                ConnectionTimeout = TimeSpan.FromSeconds(30)
            },
            Logging = new LoggingSettings
            {
                Level = "Information",
                Providers = ["Console", "File"]
            }
        };
    }
}