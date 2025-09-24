namespace Agent.Orchestration.Models;

/// <summary>
/// Defines retry behavior for failed workflow steps
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Retry strategy to use
    /// </summary>
    public RetryStrategy Strategy { get; set; } = RetryStrategy.FixedDelay;

    /// <summary>
    /// Maximum delay for exponential backoff
    /// </summary>
    public TimeSpan? MaxDelay { get; set; }

    /// <summary>
    /// Multiplier for exponential backoff
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Exception types that should trigger a retry
    /// </summary>
    public List<Type> RetryableExceptions { get; set; } = new();

    /// <summary>
    /// Exception types that should not be retried
    /// </summary>
    public List<Type> NonRetryableExceptions { get; set; } = new();
}

/// <summary>
/// Retry strategy options
/// </summary>
public enum RetryStrategy
{
    /// <summary>
    /// Fixed delay between retries
    /// </summary>
    FixedDelay,

    /// <summary>
    /// Exponential backoff delay
    /// </summary>
    ExponentialBackoff,

    /// <summary>
    /// Linear increase in delay
    /// </summary>
    LinearBackoff,

    /// <summary>
    /// Random jitter added to delay
    /// </summary>
    RandomJitter
}