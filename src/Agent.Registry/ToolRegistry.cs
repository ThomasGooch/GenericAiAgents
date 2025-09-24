using Agent.Tools;
using System.Collections.Concurrent;
using System.Reflection;

namespace Agent.Registry;

/// <summary>
/// Thread-safe tool registry implementation with reflection-based discovery
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly ConcurrentDictionary<string, ITool> _tools = new();
    private readonly object _discoveryLock = new();
    private readonly HashSet<Assembly> _scannedAssemblies = new();

    /// <inheritdoc/>
    public Task RegisterToolAsync(ITool tool, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tool);

        _tools.AddOrUpdate(tool.Name, tool, (key, existingTool) => tool);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task RegisterToolAsync(Type toolType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolType);

        if (!typeof(ITool).IsAssignableFrom(toolType))
            throw new ArgumentException($"Type {toolType.Name} does not implement ITool", nameof(toolType));

        if (toolType.IsAbstract || toolType.IsInterface)
            throw new ArgumentException($"Type {toolType.Name} cannot be abstract or interface", nameof(toolType));

        try
        {
            var instance = (ITool)Activator.CreateInstance(toolType)!;
            await RegisterToolAsync(instance, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create instance of {toolType.Name}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public Task<ITool?> GetToolAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(name));

        _tools.TryGetValue(name, out var tool);
        return Task.FromResult(tool);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ITool>> GetAllToolsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ITool>>(_tools.Values.ToList());
    }

    /// <inheritdoc/>
    public async Task DiscoverAndRegisterToolsAsync(CancellationToken cancellationToken = default)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        await DiscoverAndRegisterToolsAsync(assemblies, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DiscoverAndRegisterToolsAsync(IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var assemblyList = assemblies.ToList();
        if (!assemblyList.Any())
            return;

        // Thread-safe discovery to avoid duplicate scanning
        lock (_discoveryLock)
        {
            assemblyList = assemblyList.Where(a => !_scannedAssemblies.Contains(a)).ToList();
            foreach (var assembly in assemblyList)
            {
                _scannedAssemblies.Add(assembly);
            }
        }

        var tasks = assemblyList.Select(assembly => ScanAssemblyForToolsAsync(assembly, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public Task<bool> IsRegisteredAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(name));

        return Task.FromResult(_tools.ContainsKey(name));
    }

    /// <inheritdoc/>
    public Task<bool> UnregisterToolAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(name));

        var removed = _tools.TryRemove(name, out _);
        return Task.FromResult(removed);
    }

    private async Task ScanAssemblyForToolsAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        try
        {
            var toolTypes = assembly.GetTypes()
                .Where(type => typeof(ITool).IsAssignableFrom(type) &&
                              !type.IsAbstract &&
                              !type.IsInterface &&
                              type.GetCustomAttribute<ToolAttribute>() != null)
                .ToList();

            var registrationTasks = toolTypes.Select(type => RegisterToolAsync(type, cancellationToken));
            await Task.WhenAll(registrationTasks);
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Handle assemblies that can't be fully loaded
            var loadableTypes = ex.Types.Where(t => t != null).ToArray();

            var toolTypes = loadableTypes
                .Where(type => typeof(ITool).IsAssignableFrom(type) &&
                              !type.IsAbstract &&
                              !type.IsInterface &&
                              type.GetCustomAttribute<ToolAttribute>() != null)
                .ToList();

            var registrationTasks = toolTypes.Select(type => RegisterToolAsync(type!, cancellationToken));
            await Task.WhenAll(registrationTasks);
        }
        catch (Exception)
        {
            // Skip assemblies that can't be scanned
            // In production, this might be logged
        }
    }
}