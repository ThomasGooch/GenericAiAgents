using Agent.Configuration.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Agent.Configuration;

/// <summary>
/// Default implementation of configuration validator
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    private readonly List<ConfigurationValidationRule> _customRules = new();

    public async Task<ConfigurationValidationResult> ValidateAsync(AgentSystemConfiguration configuration, IEnumerable<ConfigurationValidationRule>? customRules = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConfigurationValidationResult { IsValid = true };

        try
        {
            // Validate using data annotations
            await ValidateDataAnnotationsAsync(configuration, result, cancellationToken);

            // Validate business rules
            await ValidateBusinessRulesAsync(configuration, result, cancellationToken);

            // Apply custom rules
            var allCustomRules = _customRules.AsEnumerable();
            if (customRules != null)
                allCustomRules = allCustomRules.Concat(customRules);

            await ValidateCustomRulesAsync(configuration, allCustomRules, result, cancellationToken);

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Validation failed with exception: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.Context["ValidationDuration"] = stopwatch.Elapsed;
        }

        return result;
    }

    public async Task<ConfigurationValidationResult> ValidateEnvironmentSpecificAsync(AgentSystemConfiguration configuration, string environment, CancellationToken cancellationToken = default)
    {
        var result = await ValidateAsync(configuration, null, cancellationToken);

        // Apply environment-specific validations
        switch (environment.ToLowerInvariant())
        {
            case "production":
                ValidateProductionEnvironment(configuration, result);
                break;
            case "staging":
                ValidateStagingEnvironment(configuration, result);
                break;
            case "development":
                ValidateDevelopmentEnvironment(configuration, result);
                break;
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    public void AddValidationRule(ConfigurationValidationRule rule)
    {
        _customRules.Add(rule);
    }

    public void RemoveValidationRule(string fieldName)
    {
        _customRules.RemoveAll(r => r.Field.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<ConfigurationValidationRule> GetValidationRules()
    {
        return _customRules.AsReadOnly();
    }

    private async Task ValidateDataAnnotationsAsync(AgentSystemConfiguration configuration, ConfigurationValidationResult result, CancellationToken cancellationToken)
    {
        var context = new ValidationContext(configuration);
        var validationResults = new List<ValidationResult>();

        if (!ValidationExtensions.TryValidateObjectRecursively(configuration, context, validationResults))
        {
            foreach (var validationResult in validationResults)
            {
                var fieldName = validationResult.MemberNames.FirstOrDefault() ?? "Unknown";
                result.Errors.Add($"{fieldName}: {validationResult.ErrorMessage}");
            }
        }

        await Task.CompletedTask; // For async compliance
    }

    private async Task ValidateBusinessRulesAsync(AgentSystemConfiguration configuration, ConfigurationValidationResult result, CancellationToken cancellationToken)
    {
        // Only add business rule errors if data annotation validation didn't already catch them
        var existingErrors = new HashSet<string>(result.Errors);

        // Validate system settings - only if not already caught by data annotations
        if (string.IsNullOrWhiteSpace(configuration.AgentSystem.Name) && 
            !existingErrors.Any(e => e.Contains("AgentSystem.Name")))
        {
            result.Errors.Add("AgentSystem.Name: Name cannot be empty or whitespace");
        }

        if (!IsValidSemanticVersion(configuration.AgentSystem.Version) && 
            !existingErrors.Any(e => e.Contains("AgentSystem.Version") && e.Contains("semantic version")))
        {
            result.Errors.Add("AgentSystem.Version: Version must be in semantic version format (e.g., 1.0.0)");
        }

        // Validate agent settings - only if not already caught by data annotations
        if (configuration.Agents.MaxConcurrentAgents <= 0 && 
            !existingErrors.Any(e => e.Contains("Agents.MaxConcurrentAgents")))
        {
            result.Errors.Add("Agents.MaxConcurrentAgents: MaxConcurrentAgents must be positive");
        }

        if (configuration.Agents.DefaultTimeout <= TimeSpan.Zero)
        {
            result.Errors.Add("Agents.DefaultTimeout: DefaultTimeout cannot be negative or zero");
        }

        if (configuration.Agents.HealthCheckInterval < TimeSpan.FromSeconds(10))
        {
            result.Warnings.Add("Agents.HealthCheckInterval: Health check interval less than 10 seconds may cause performance issues");
        }

        // Validate database settings
        if (configuration.Database != null)
        {
            if (string.IsNullOrWhiteSpace(configuration.Database.ConnectionString) && 
                !existingErrors.Any(e => e.Contains("Database.ConnectionString")))
            {
                result.Errors.Add("Database.ConnectionString: Connection string cannot be empty");
            }

            if (configuration.Database.MaxConnections <= 0 && 
                !existingErrors.Any(e => e.Contains("Database.MaxConnections")))
            {
                result.Errors.Add("Database.MaxConnections: MaxConnections must be positive");
            }

            if (configuration.Database.ConnectionTimeout > TimeSpan.FromMinutes(2))
            {
                result.Warnings.Add("Database.ConnectionTimeout: Connection timeout greater than 2 minutes may cause client timeouts");
            }
        }

        // Validate logging settings
        var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
        if (!validLogLevels.Contains(configuration.Logging.Level))
        {
            result.Errors.Add($"Logging.Level: Invalid log level '{configuration.Logging.Level}'. Valid levels are: {string.Join(", ", validLogLevels)}");
        }

        await Task.CompletedTask; // For async compliance
    }

    private async Task ValidateCustomRulesAsync(AgentSystemConfiguration configuration, IEnumerable<ConfigurationValidationRule> customRules, ConfigurationValidationResult result, CancellationToken cancellationToken)
    {
        foreach (var rule in customRules)
        {
            try
            {
                var fieldValue = GetFieldValue(configuration, rule.Field);
                if (!rule.Rule(fieldValue))
                {
                    if (rule.IsWarning)
                    {
                        result.Warnings.Add($"{rule.Field}: {rule.ErrorMessage}");
                    }
                    else
                    {
                        result.Errors.Add($"{rule.Field}: {rule.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Custom rule validation failed for '{rule.Field}': {ex.Message}");
            }
        }

        await Task.CompletedTask; // For async compliance
    }

    private void ValidateProductionEnvironment(AgentSystemConfiguration configuration, ConfigurationValidationResult result)
    {
        // Production-specific validations
        if (configuration.Agents.MaxConcurrentAgents < 10)
        {
            result.Errors.Add("Agents.MaxConcurrentAgents: Production environment should have at least 10 concurrent agents");
        }

        if (configuration.Agents.DefaultTimeout < TimeSpan.FromMinutes(1))
        {
            result.Errors.Add("Agents.DefaultTimeout: Production environment should have timeout of at least 1 minute");
        }

        if (configuration.Logging.Level == "Debug" || configuration.Logging.Level == "Trace")
        {
            result.Warnings.Add("Logging.Level: Debug/Trace logging not recommended for production environments");
        }

        if (configuration.Security?.RequireHttps == false)
        {
            result.Errors.Add("Security.RequireHttps: HTTPS is required in production environment");
        }
    }

    private void ValidateStagingEnvironment(AgentSystemConfiguration configuration, ConfigurationValidationResult result)
    {
        // Staging-specific validations
        if (configuration.Agents.MaxConcurrentAgents < 5)
        {
            result.Warnings.Add("Agents.MaxConcurrentAgents: Staging environment should have at least 5 concurrent agents for realistic testing");
        }

        if (configuration.Monitoring?.Enabled == false)
        {
            result.Warnings.Add("Monitoring.Enabled: Monitoring should be enabled in staging environment");
        }
    }

    private void ValidateDevelopmentEnvironment(AgentSystemConfiguration configuration, ConfigurationValidationResult result)
    {
        // Development-specific validations
        if (configuration.Database != null && configuration.Database.ConnectionString.Contains("production", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("Database.ConnectionString: Development environment should not connect to production database");
        }

        if (configuration.Logging.Level == "Warning" || configuration.Logging.Level == "Error")
        {
            result.Warnings.Add("Logging.Level: Consider using Debug or Information level in development environment");
        }
    }

    private static bool IsValidSemanticVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        var semVerPattern = @"^\d+\.\d+\.\d+(-[a-zA-Z0-9.-]+)?(\+[a-zA-Z0-9.-]+)?$";
        return Regex.IsMatch(version, semVerPattern);
    }

    private static object? GetFieldValue(object obj, string fieldPath)
    {
        var parts = fieldPath.Split('.');
        object? current = obj;

        foreach (var part in parts)
        {
            if (current == null)
                return null;

            var property = current.GetType().GetProperty(part);
            if (property == null)
                throw new ArgumentException($"Property '{part}' not found in type '{current.GetType().Name}'");

            current = property.GetValue(current);
        }

        return current;
    }
}

/// <summary>
/// Extension methods for recursive validation
/// </summary>
public static class ValidationExtensions
{
    public static bool TryValidateObjectRecursively<T>(T obj, ValidationContext context, List<ValidationResult> results)
    {
        if (obj == null) return false;

        bool isValid = true;
        var visited = new HashSet<object>();

        isValid &= ValidateObjectRecursive(obj, "", context, results, visited);

        return isValid;
    }

    private static bool ValidateObjectRecursive(object obj, string prefix, ValidationContext context, List<ValidationResult> results, HashSet<object> visited)
    {
        if (obj == null || visited.Contains(obj))
            return true;

        // Add to visited to prevent circular references
        visited.Add(obj);

        bool isValid = true;

        // Validate current object with data annotations
        var currentContext = new ValidationContext(obj);
        var currentResults = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(obj, currentContext, currentResults, true))
        {
            isValid = false;
            foreach (var result in currentResults)
            {
                var memberNames = result.MemberNames.Select(name => 
                    string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}").ToArray();
                
                results.Add(new ValidationResult(result.ErrorMessage, memberNames));
            }
        }

        // Recursively validate properties
        var properties = obj.GetType().GetProperties()
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        foreach (var property in properties)
        {
            var value = property.GetValue(obj);
            if (value == null) continue;

            var propertyName = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

            // Skip primitive types, strings, and DateTime
            var propertyType = property.PropertyType;
            if (propertyType.IsPrimitive || 
                propertyType == typeof(string) || 
                propertyType == typeof(DateTime) || 
                propertyType == typeof(TimeSpan) ||
                propertyType == typeof(Guid) ||
                propertyType.IsEnum)
            {
                continue;
            }

            // Handle collections
            if (value is System.Collections.IEnumerable enumerable && propertyType != typeof(string))
            {
                int index = 0;
                foreach (var item in enumerable)
                {
                    if (item != null && !IsSimpleType(item.GetType()))
                    {
                        isValid &= ValidateObjectRecursive(item, $"{propertyName}[{index}]", context, results, visited);
                    }
                    index++;
                }
            }
            // Handle complex objects
            else if (!IsSimpleType(propertyType))
            {
                isValid &= ValidateObjectRecursive(value, propertyName, context, results, visited);
            }
        }

        return isValid;
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(TimeSpan) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(Guid) ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                IsSimpleType(type.GetGenericArguments()[0]));
    }
}