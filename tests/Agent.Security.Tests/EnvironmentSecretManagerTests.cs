using Agent.Security.Implementations;
using Agent.Security.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Microsoft.Extensions.Options;
using Xunit;

namespace Agent.Security.Tests;

[CollectionDefinition("EnvironmentVariableTests", DisableParallelization = true)]
public class EnvironmentVariableTestCollection { }

[Collection("EnvironmentVariableTests")]
public class EnvironmentSecretManagerTests : IDisposable
{
    private readonly ILogger<EnvironmentSecretManager> _logger;
    private readonly EnvironmentSecretManager _secretManager;
    private readonly List<string> _environmentVariablesToCleanup = new();

    public EnvironmentSecretManagerTests()
    {
        // Clean up any leftover test environment variables first
        var envVars = Environment.GetEnvironmentVariables();
        foreach (string key in envVars.Keys)
        {
            if (key.StartsWith("TEST_SECRET_", StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable(key, null);
            }
        }
        
        _logger = Substitute.For<ILogger<EnvironmentSecretManager>>();
        var options = Options.Create(new SecretManagerOptions
        {
            EnvironmentPrefix = "TEST_SECRET_",
            EnableCaching = false
        });
        _secretManager = new EnvironmentSecretManager(_logger, options);
    }

    [Fact]
    public async Task GetSecretAsync_WithExistingSecret_ShouldReturnValue()
    {
        // Arrange
        var secretName = "DATABASE_PASSWORD";
        var secretValue = "test-password-123";
        var envVarName = "TEST_SECRET_DATABASE_PASSWORD";
        Environment.SetEnvironmentVariable(envVarName, secretValue);
        _environmentVariablesToCleanup.Add(envVarName);

        // Act
        var result = await _secretManager.GetSecretAsync(secretName);

        // Assert
        Assert.Equal(secretValue, result);
    }

    [Fact]
    public async Task GetSecretAsync_WithMissingSecret_ShouldReturnNull()
    {
        // Arrange
        var secretName = "nonexistent-secret";

        // Act
        var result = await _secretManager.GetSecretAsync(secretName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetSecretAsync_ShouldSetEnvironmentVariable()
    {
        // Arrange
        var secretName = "NEW_SECRET";
        var secretValue = "new-value";
        var envVarName = "TEST_SECRET_NEW_SECRET";
        _environmentVariablesToCleanup.Add(envVarName);

        // Act
        await _secretManager.SetSecretAsync(secretName, secretValue);

        // Assert
        var result = Environment.GetEnvironmentVariable(envVarName);
        Assert.Equal(secretValue, result);
    }

    [Fact]
    public async Task DeleteSecretAsync_ShouldRemoveEnvironmentVariable()
    {
        // Arrange
        var secretName = "TEMP_SECRET";
        var envVarName = "TEST_SECRET_TEMP_SECRET";
        Environment.SetEnvironmentVariable(envVarName, "temp-value");
        _environmentVariablesToCleanup.Add(envVarName);

        // Act
        await _secretManager.DeleteSecretAsync(secretName);

        // Assert
        var result = Environment.GetEnvironmentVariable(envVarName);
        Assert.Null(result);
    }

    [Fact]
    public async Task SecretExistsAsync_WithExistingSecret_ShouldReturnTrue()
    {
        // Arrange
        var secretName = "EXISTING_SECRET";
        var envVarName = "TEST_SECRET_EXISTING_SECRET";
        Environment.SetEnvironmentVariable(envVarName, "some-value");
        _environmentVariablesToCleanup.Add(envVarName);

        // Act
        var result = await _secretManager.SecretExistsAsync(secretName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SecretExistsAsync_WithMissingSecret_ShouldReturnFalse()
    {
        // Arrange
        var secretName = "missing-secret";

        // Act
        var result = await _secretManager.SecretExistsAsync(secretName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ListSecretNamesAsync_ShouldReturnSecretsWithPrefix()
    {
        // Arrange
        var secrets = new Dictionary<string, string>
        {
            ["TEST_SECRET_SECRET1"] = "value1",
            ["TEST_SECRET_SECRET2"] = "value2",
            ["OTHER_SECRET"] = "value3" // Should not be included
        };

        foreach (var secret in secrets)
        {
            Environment.SetEnvironmentVariable(secret.Key, secret.Value);
            _environmentVariablesToCleanup.Add(secret.Key);
        }

        // Act
        var result = await _secretManager.ListSecretNamesAsync();

        // Assert
        var secretNames = result.ToList();
        Assert.Contains("SECRET1", secretNames);
        Assert.Contains("SECRET2", secretNames);
        Assert.DoesNotContain("OTHER_SECRET", secretNames);
        Assert.Equal(2, secretNames.Count);
    }

    public void Dispose()
    {
        // Clean up environment variables
        foreach (var envVar in _environmentVariablesToCleanup)
        {
            try
            {
                Environment.SetEnvironmentVariable(envVar, null);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        // Additional cleanup - remove any TEST_SECRET_ variables
        var envVars = Environment.GetEnvironmentVariables();
        foreach (string key in envVars.Keys)
        {
            if (key.StartsWith("TEST_SECRET_", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Environment.SetEnvironmentVariable(key, null);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}