using Agent.Security.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent.Security.Implementations;

/// <summary>
/// Environment variable-based secret manager for development and testing
/// </summary>
public class EnvironmentSecretManager : ISecretManager
{
    private readonly ILogger<EnvironmentSecretManager> _logger;
    private readonly SecretManagerOptions _options;

    public EnvironmentSecretManager(
        ILogger<EnvironmentSecretManager> logger,
        IOptions<SecretManagerOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            var envVarName = GetEnvironmentVariableName(secretName);
            var secretValue = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrEmpty(secretValue))
            {
                _logger.LogWarning("Secret '{SecretName}' not found in environment variable '{EnvVar}'", 
                    secretName, envVarName);
                return Task.FromResult<string?>(null);
            }

            _logger.LogDebug("Retrieved secret '{SecretName}' from environment", secretName);
            return Task.FromResult<string?>(secretValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret '{SecretName}' from environment", secretName);
            throw;
        }
    }

    public Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        try
        {
            var envVarName = GetEnvironmentVariableName(secretName);
            Environment.SetEnvironmentVariable(envVarName, secretValue);
            
            _logger.LogDebug("Set secret '{SecretName}' in environment", secretName);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret '{SecretName}' in environment", secretName);
            throw;
        }
    }

    public Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            var envVarName = GetEnvironmentVariableName(secretName);
            Environment.SetEnvironmentVariable(envVarName, null);
            
            _logger.LogDebug("Deleted secret '{SecretName}' from environment", secretName);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret '{SecretName}' from environment", secretName);
            throw;
        }
    }

    public Task<IEnumerable<string>> ListSecretNamesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var envVars = Environment.GetEnvironmentVariables();
            var secretNames = new List<string>();

            foreach (string key in envVars.Keys)
            {
                if (key.StartsWith(_options.EnvironmentPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var secretName = key.Substring(_options.EnvironmentPrefix.Length);
                    secretNames.Add(secretName);
                }
            }

            _logger.LogDebug("Found {Count} secrets in environment variables", secretNames.Count);
            return Task.FromResult<IEnumerable<string>>(secretNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list secret names from environment");
            throw;
        }
    }

    public Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            var envVarName = GetEnvironmentVariableName(secretName);
            var exists = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVarName));
            
            _logger.LogDebug("Secret '{SecretName}' exists: {Exists}", secretName, exists);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if secret '{SecretName}' exists", secretName);
            throw;
        }
    }

    private string GetEnvironmentVariableName(string secretName)
    {
        return $"{_options.EnvironmentPrefix}{secretName.ToUpperInvariant()}";
    }
}