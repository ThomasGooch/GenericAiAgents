using Agent.Security.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent.Security.Implementations;

/// <summary>
/// Azure Key Vault-based secret manager for production environments
/// </summary>
public class AzureKeyVaultSecretManager : ISecretManager
{
    private readonly ILogger<AzureKeyVaultSecretManager> _logger;
    private readonly SecretClient _secretClient;
    private readonly AzureKeyVaultOptions _options;

    public AzureKeyVaultSecretManager(
        ILogger<AzureKeyVaultSecretManager> logger,
        IOptions<SecretManagerOptions> options)
    {
        _logger = logger;
        var secretManagerOptions = options.Value;
        _options = secretManagerOptions.AzureKeyVault 
            ?? throw new InvalidOperationException("AzureKeyVault options are required when using AzureKeyVault secret manager");

        // Create credential based on configuration
        var credential = CreateCredential();
        
        // Create secret client
        _secretClient = new SecretClient(new Uri(_options.VaultUrl), credential);
        
        _logger.LogInformation("Initialized Azure Key Vault secret manager with vault: {VaultUrl}", _options.VaultUrl);
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            
            if (response?.Value?.Value == null)
            {
                _logger.LogWarning("Secret '{SecretName}' not found in Azure Key Vault", secretName);
                return null;
            }

            _logger.LogDebug("Retrieved secret '{SecretName}' from Azure Key Vault", secretName);
            return response.Value.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret '{SecretName}' not found in Azure Key Vault", secretName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret '{SecretName}' from Azure Key Vault", secretName);
            throw;
        }
    }

    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        try
        {
            var secret = new KeyVaultSecret(secretName, secretValue);
            await _secretClient.SetSecretAsync(secret, cancellationToken);
            
            _logger.LogDebug("Set secret '{SecretName}' in Azure Key Vault", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret '{SecretName}' in Azure Key Vault", secretName);
            throw;
        }
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);
            
            _logger.LogDebug("Deleted secret '{SecretName}' from Azure Key Vault", secretName);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret '{SecretName}' not found for deletion in Azure Key Vault", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret '{SecretName}' from Azure Key Vault", secretName);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListSecretNamesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var secretNames = new List<string>();
            
            await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
            {
                if (!secretProperties.Enabled.GetValueOrDefault(true))
                    continue;
                    
                secretNames.Add(secretProperties.Name);
            }

            _logger.LogDebug("Found {Count} secrets in Azure Key Vault", secretNames.Count);
            return secretNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list secret names from Azure Key Vault");
            throw;
        }
    }

    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            
            _logger.LogDebug("Secret '{SecretName}' exists in Azure Key Vault", secretName);
            return true;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogDebug("Secret '{SecretName}' does not exist in Azure Key Vault", secretName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if secret '{SecretName}' exists in Azure Key Vault", secretName);
            throw;
        }
    }

    private Azure.Core.TokenCredential CreateCredential()
    {
        if (_options.UseManagedIdentity)
        {
            _logger.LogDebug("Using managed identity for Azure Key Vault authentication");
            return new ManagedIdentityCredential();
        }

        if (!string.IsNullOrEmpty(_options.ClientId) && !string.IsNullOrEmpty(_options.TenantId))
        {
            _logger.LogDebug("Using service principal for Azure Key Vault authentication");
            return new ClientSecretCredential(_options.TenantId, _options.ClientId, 
                Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") 
                ?? throw new InvalidOperationException("AZURE_CLIENT_SECRET environment variable is required when using service principal authentication"));
        }

        _logger.LogDebug("Using default Azure credential for Azure Key Vault authentication");
        return new DefaultAzureCredential();
    }
}