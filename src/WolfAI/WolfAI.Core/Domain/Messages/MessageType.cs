namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Enumeration of message types.
/// </summary>
public enum MessageType
{
    /// <summary>User input message.</summary>
    Human,

    /// <summary>AI response message.</summary>
    AI,

    /// <summary>Tool output message.</summary>
    Tool,

    /// <summary>System message for instructions.</summary>
    System,

    /// <summary>Function call message.</summary>
    Function,

    /// <summary>Removal message.</summary>
    Remove
}
