using Agent.Core;

namespace Agent.Orchestration.Models;

/// <summary>
/// Health status levels
/// </summary>
public enum HealthLevel
{
    /// <summary>
    /// Health status unknown
    /// </summary>
    Unknown,

    /// <summary>
    /// Agent is healthy and fully operational
    /// </summary>
    Healthy,

    /// <summary>
    /// Agent is operational but with warnings
    /// </summary>
    Warning,

    /// <summary>
    /// Agent is degraded but still functional
    /// </summary>
    Degraded,

    /// <summary>
    /// Agent is unhealthy and not functional
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Agent is completely unavailable
    /// </summary>
    Critical
}