namespace WolfAI.Core.Domain.Nodes;

/// <summary>
/// Enumeration of node types in the graph.
/// </summary>
public enum NodeType
{
    /// <summary>Graph entry point</summary>
    Start,
    
    /// <summary>Graph exit point</summary>
    End,
    
    /// <summary>AI-driven node with executable logic</summary>
    AI,
    
    /// <summary>Tool invocation node</summary>
    Tool
}
