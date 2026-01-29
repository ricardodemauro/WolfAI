using System.Collections.ObjectModel;

namespace WolfAI.Core.Domain.Edges;

/// <summary>
/// Metadata associated with an edge.
/// </summary>
public class EdgeMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EdgeMetadata"/> class.
    /// </summary>
    public EdgeMetadata(string? description = null, IReadOnlyList<string>? tags = null)
    {
        Description = description;
        Tags = new ReadOnlyCollection<string>((tags ?? []).ToArray());
    }

    /// <summary>
    /// Gets the description of the edge.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the tags associated with the edge.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }
}
