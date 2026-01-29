using System.Collections.ObjectModel;
using FluentAssertions;
using WolfAI.Core.Domain.Edges;

namespace WolfAI.Tests.Domain.Edges;

public class EdgeMetadataTests
{
    [Fact]
    public void EdgeMetadata_Constructor_Sets_Properties()
    {
        // Arrange & Act
        var metadata = new EdgeMetadata("Test edge", new[] { "tag1", "tag2" });

        // Assert
        metadata.Description.Should().Be("Test edge");
        metadata.Tags.Should().HaveCount(2).And.Contain("tag1", "tag2");
    }

    [Fact]
    public void EdgeMetadata_With_Null_Description_Sets_Null()
    {
        // Arrange & Act
        var metadata = new EdgeMetadata();

        // Assert
        metadata.Description.Should().BeNull();
        metadata.Tags.Should().BeEmpty();
    }

    [Fact]
    public void EdgeMetadata_Tags_Are_Immutable()
    {
        // Arrange & Act
        var tags = new[] { "tag1", "tag2" };
        var metadata = new EdgeMetadata("Test", tags);

        // Assert
        metadata.Tags.Should().BeOfType<ReadOnlyCollection<string>>();
    }
}
