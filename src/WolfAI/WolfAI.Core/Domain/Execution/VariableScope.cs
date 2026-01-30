using System.Collections.Concurrent;

namespace WolfAI.Core.Domain.Execution;

/// <summary>
/// Manages hierarchical variable scoping for graph execution.
/// Supports both global (graph-level) and node-scoped variables with efficient resolution.
/// </summary>
public class VariableScope
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object?>> _nodeVariables;
    private readonly IDictionary<string, object?> _globalVariables;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableScope"/> class.
    /// </summary>
    /// <param name="globalVariables">Reference to the global variables dictionary</param>
    public VariableScope(IDictionary<string, object?> globalVariables)
    {
        _globalVariables = globalVariables ?? throw new ArgumentNullException(nameof(globalVariables));
        _nodeVariables = new ConcurrentDictionary<string, ConcurrentDictionary<string, object?>>();
    }

    /// <summary>
    /// Gets a global variable by key.
    /// </summary>
    /// <param name="key">The variable key</param>
    /// <returns>The variable value</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not found</exception>
    public object? GetGlobal(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        if (_globalVariables.TryGetValue(key, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException($"Global variable '{key}' not found.");
    }

    /// <summary>
    /// Sets a global variable.
    /// </summary>
    /// <param name="key">The variable key</param>
    /// <param name="value">The variable value</param>
    public void SetGlobal(string key, object? value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        _globalVariables[key] = value;
    }

    /// <summary>
    /// Gets a node-scoped variable.
    /// </summary>
    /// <param name="nodeId">The node ID</param>
    /// <param name="key">The variable key</param>
    /// <returns>The variable value</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not found</exception>
    public object? GetNodeVariable(string nodeId, string key)
    {
        if (nodeId == null) throw new ArgumentNullException(nameof(nodeId));
        if (key == null) throw new ArgumentNullException(nameof(key));

        if (_nodeVariables.TryGetValue(nodeId, out var nodeVars) &&
            nodeVars.TryGetValue(key, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException($"Node variable '{key}' for node '{nodeId}' not found.");
    }

    /// <summary>
    /// Sets a node-scoped variable.
    /// </summary>
    /// <param name="nodeId">The node ID</param>
    /// <param name="key">The variable key</param>
    /// <param name="value">The variable value</param>
    public void SetNodeVariable(string nodeId, string key, object? value)
    {
        if (nodeId == null) throw new ArgumentNullException(nameof(nodeId));
        if (key == null) throw new ArgumentNullException(nameof(key));

        var nodeVars = _nodeVariables.GetOrAdd(nodeId, _ => new ConcurrentDictionary<string, object?>());
        nodeVars[key] = value;
    }

    /// <summary>
    /// Tries to get a variable, checking node-scoped first, then global.
    /// </summary>
    /// <param name="key">The variable key</param>
    /// <param name="nodeId">Optional: the node ID to check for node-scoped variables</param>
    /// <param name="value">The variable value if found</param>
    /// <returns>True if the variable was found, false otherwise</returns>
    public bool TryGetVariable(string key, out object? value, string? nodeId = null)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        // Try node-scoped first if nodeId is provided
        if (!string.IsNullOrEmpty(nodeId))
        {
            if (_nodeVariables.TryGetValue(nodeId, out var nodeVars) &&
                nodeVars.TryGetValue(key, out value))
            {
                return true;
            }
        }

        // Fall back to global
        return _globalVariables.TryGetValue(key, out value);
    }

    /// <summary>
    /// Gets all global variables.
    /// </summary>
    /// <returns>The global variables dictionary</returns>
    public IReadOnlyDictionary<string, object?> GetAllGlobalVariables()
    {
        return _globalVariables.ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>
    /// Gets all node-scoped variables for a specific node.
    /// </summary>
    /// <param name="nodeId">The node ID</param>
    /// <returns>The node variables dictionary, or empty if none exist</returns>
    public IReadOnlyDictionary<string, object?> GetAllNodeVariables(string nodeId)
    {
        if (nodeId == null) throw new ArgumentNullException(nameof(nodeId));

        if (_nodeVariables.TryGetValue(nodeId, out var nodeVars))
        {
            return nodeVars.ToDictionary(x => x.Key, x => x.Value);
        }

        return new Dictionary<string, object?>();
    }

    /// <summary>
    /// Clears all node-scoped variables for a specific node.
    /// </summary>
    /// <param name="nodeId">The node ID</param>
    public void ClearNodeVariables(string nodeId)
    {
        if (nodeId == null) throw new ArgumentNullException(nameof(nodeId));
        _nodeVariables.TryRemove(nodeId, out _);
    }
}
