using Agent.Security.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent.Security.Implementations;

/// <summary>
/// Decorator that adds caching to any ISecretManager implementation
/// </summary>
public class CachedSecretManager : ISecretManager
{
    private readonly ISecretManager _innerSecretManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedSecretManager> _logger;
    private readonly SecretManagerOptions _options;

    public CachedSecretManager(
        ISecretManager innerSecretManager,
        IMemoryCache cache,
        ILogger<CachedSecretManager> logger,
        IOptions<SecretManagerOptions> options)
    {
        _innerSecretManager = innerSecretManager;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableCaching)
        {
            return await _innerSecretManager.GetSecretAsync(secretName, cancellationToken);
        }

        var cacheKey = GetCacheKey(secretName);

        if (_cache.TryGetValue(cacheKey, out string? cachedValue))
        {
            _logger.LogDebug("Retrieved secret '{SecretName}' from cache", secretName);
            return cachedValue;
        }

        var secretValue = await _innerSecretManager.GetSecretAsync(secretName, cancellationToken);

        if (secretValue != null)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.CacheExpiration,
                Priority = CacheItemPriority.High
            };

            _cache.Set(cacheKey, secretValue, cacheEntryOptions);
            _logger.LogDebug("Cached secret '{SecretName}' for {Expiration}", secretName, _options.CacheExpiration);
        }

        return secretValue;
    }

    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        await _innerSecretManager.SetSecretAsync(secretName, secretValue, cancellationToken);

        if (_options.EnableCaching)
        {
            var cacheKey = GetCacheKey(secretName);
            _cache.Remove(cacheKey);
            _logger.LogDebug("Removed secret '{SecretName}' from cache after update", secretName);
        }
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        await _innerSecretManager.DeleteSecretAsync(secretName, cancellationToken);

        if (_options.EnableCaching)
        {
            var cacheKey = GetCacheKey(secretName);
            _cache.Remove(cacheKey);
            _logger.LogDebug("Removed secret '{SecretName}' from cache after deletion", secretName);
        }
    }

    public Task<IEnumerable<string>> ListSecretNamesAsync(CancellationToken cancellationToken = default)
    {
        // Don't cache secret names as they change infrequently and caching complicates invalidation
        return _innerSecretManager.ListSecretNamesAsync(cancellationToken);
    }

    public Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        // Don't cache existence checks as they're typically used before get operations
        return _innerSecretManager.SecretExistsAsync(secretName, cancellationToken);
    }

    private string GetCacheKey(string secretName)
    {
        return $"secret:{secretName}";
    }
}