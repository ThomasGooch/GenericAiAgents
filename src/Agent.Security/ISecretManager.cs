namespace Agent.Security;

/// <summary>
/// Abstraction for secret management that can work with various backends
/// (Environment variables, Azure Key Vault, AWS Secrets Manager, etc.)
/// </summary>
public interface ISecretManager
{
    /// <summary>
    /// Retrieves a secret value by name
    /// </summary>
    /// <param name="secretName">Name of the secret</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The secret value or null if not found</returns>
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores or updates a secret value
    /// </summary>
    /// <param name="secretName">Name of the secret</param>
    /// <param name="secretValue">Value to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a secret
    /// </summary>
    /// <param name="secretName">Name of the secret to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available secret names
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of secret names</returns>
    Task<IEnumerable<string>> ListSecretNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a secret exists
    /// </summary>
    /// <param name="secretName">Name of the secret</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if secret exists, false otherwise</returns>
    Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default);
}