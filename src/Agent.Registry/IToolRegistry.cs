using Agent.Tools;
using System.Reflection;

namespace Agent.Registry;

/// <summary>
/// Manages tool registration and discovery for the agent system
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Registers a tool instance with the registry
    /// </summary>
    /// <param name="tool">The tool to register</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterToolAsync(ITool tool, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a tool by its type
    /// </summary>
    /// <param name="toolType">The tool type to register</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterToolAsync(Type toolType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tool by name
    /// </summary>
    /// <param name="name">The tool name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tool instance or null if not found</returns>
    Task<ITool?> GetToolAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered tools
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all registered tools</returns>
    Task<IEnumerable<ITool>> GetAllToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers and registers tools from the current app domain assemblies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DiscoverAndRegisterToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers and registers tools from specific assemblies
    /// </summary>
    /// <param name="assemblies">Assemblies to scan for tools</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DiscoverAndRegisterToolsAsync(IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tool is registered
    /// </summary>
    /// <param name="name">The tool name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the tool is registered</returns>
    Task<bool> IsRegisteredAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a tool
    /// </summary>
    /// <param name="name">The tool name to unregister</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the tool was removed</returns>
    Task<bool> UnregisterToolAsync(string name, CancellationToken cancellationToken = default);
}