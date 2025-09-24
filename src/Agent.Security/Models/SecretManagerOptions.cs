namespace Agent.Security.Models;

/// <summary>
/// Configuration options for secret management
/// </summary>
public class SecretManagerOptions
{
    /// <summary>
    /// The type of secret manager to use
    /// </summary>
    public SecretManagerType Type { get; set; } = SecretManagerType.Environment;

    /// <summary>
    /// Azure Key Vault configuration (when Type = AzureKeyVault)
    /// </summary>
    public AzureKeyVaultOptions? AzureKeyVault { get; set; }

    /// <summary>
    /// AWS Secrets Manager configuration (when Type = AwsSecretsManager)
    /// </summary>
    public AwsSecretsManagerOptions? AwsSecretsManager { get; set; }

    /// <summary>
    /// HashiCorp Vault configuration (when Type = HashiCorpVault)
    /// </summary>
    public HashiCorpVaultOptions? HashiCorpVault { get; set; }

    /// <summary>
    /// Environment variable prefix for development/testing
    /// </summary>
    public string EnvironmentPrefix { get; set; } = "AGENT_SECRET_";

    /// <summary>
    /// Whether to cache secrets in memory (default: true for performance)
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time (default: 15 minutes)
    /// </summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Types of secret managers supported
/// </summary>
public enum SecretManagerType
{
    Environment,
    AzureKeyVault,
    AwsSecretsManager,
    HashiCorpVault
}

/// <summary>
/// Azure Key Vault specific configuration
/// </summary>
public class AzureKeyVaultOptions
{
    /// <summary>
    /// Key Vault URL (e.g., https://your-vault.vault.azure.net/)
    /// </summary>
    public string VaultUrl { get; set; } = string.Empty;

    /// <summary>
    /// Azure tenant ID (optional, can use managed identity)
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Client ID for service principal authentication (optional)
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Whether to use managed identity (recommended for production)
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;
}

/// <summary>
/// AWS Secrets Manager specific configuration
/// </summary>
public class AwsSecretsManagerOptions
{
    /// <summary>
    /// AWS region
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// AWS access key (optional, can use IAM roles)
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// Secret prefix for organization
    /// </summary>
    public string SecretPrefix { get; set; } = "agent-system/";
}

/// <summary>
/// HashiCorp Vault specific configuration
/// </summary>
public class HashiCorpVaultOptions
{
    /// <summary>
    /// Vault server URL
    /// </summary>
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Secret path/mount point
    /// </summary>
    public string SecretPath { get; set; } = "secret/";

    /// <summary>
    /// Authentication method
    /// </summary>
    public VaultAuthMethod AuthMethod { get; set; } = VaultAuthMethod.Token;

    /// <summary>
    /// Vault token (for token auth)
    /// </summary>
    public string? Token { get; set; }
}

public enum VaultAuthMethod
{
    Token,
    Kubernetes,
    AppRole
}