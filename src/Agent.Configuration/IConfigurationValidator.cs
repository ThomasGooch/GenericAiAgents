using Agent.Configuration.Models;

namespace Agent.Configuration;

/// <summary>
/// Validates configuration objects and provides validation rules
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validates a configuration object
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <param name="customRules">Additional custom validation rules</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ConfigurationValidationResult> ValidateAsync(AgentSystemConfiguration configuration, IEnumerable<ConfigurationValidationRule>? customRules = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates configuration for a specific environment
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <param name="environment">Target environment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ConfigurationValidationResult> ValidateEnvironmentSpecificAsync(AgentSystemConfiguration configuration, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a custom validation rule
    /// </summary>
    /// <param name="rule">Validation rule to add</param>
    void AddValidationRule(ConfigurationValidationRule rule);

    /// <summary>
    /// Removes a validation rule
    /// </summary>
    /// <param name="fieldName">Field name of the rule to remove</param>
    void RemoveValidationRule(string fieldName);

    /// <summary>
    /// Gets all registered validation rules
    /// </summary>
    /// <returns>Collection of validation rules</returns>
    IEnumerable<ConfigurationValidationRule> GetValidationRules();
}

/// <summary>
/// Custom validation rule for configuration fields
/// </summary>
public class ConfigurationValidationRule
{
    /// <summary>
    /// Field path for the rule (e.g., "AgentSystem.Name")
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Validation function
    /// </summary>
    public Func<object?, bool> Rule { get; set; } = _ => true;

    /// <summary>
    /// Error message when validation fails
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a warning (true) or error (false)
    /// </summary>
    public bool IsWarning { get; set; } = false;

    /// <summary>
    /// Environment this rule applies to (null for all environments)
    /// </summary>
    public string? Environment { get; set; }
}

/// <summary>
/// Validation error or warning details
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// Field that failed validation
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Validation error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Current value that failed validation
    /// </summary>
    public object? CurrentValue { get; set; }

    /// <summary>
    /// Severity of the validation issue
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
}

/// <summary>
/// Validation severity levels
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message
    /// </summary>
    Info,

    /// <summary>
    /// Warning that should be addressed
    /// </summary>
    Warning,

    /// <summary>
    /// Error that prevents system operation
    /// </summary>
    Error,

    /// <summary>
    /// Critical error that requires immediate attention
    /// </summary>
    Critical
}

/// <summary>
/// Enhanced validation result with detailed issue information
/// </summary>
public class EnhancedValidationResult : ConfigurationValidationResult
{
    /// <summary>
    /// Detailed validation issues
    /// </summary>
    public List<ValidationIssue> Issues { get; set; } = new();

    /// <summary>
    /// Performance metrics for validation
    /// </summary>
    public ValidationMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Validation performance metrics
/// </summary>
public class ValidationMetrics
{
    /// <summary>
    /// Time taken to complete validation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Number of rules evaluated
    /// </summary>
    public int RulesEvaluated { get; set; }

    /// <summary>
    /// Number of fields validated
    /// </summary>
    public int FieldsValidated { get; set; }

    /// <summary>
    /// Timestamp when validation started
    /// </summary>
    public DateTime StartTime { get; set; }
}