using System.Collections.ObjectModel;
using FluentAssertions;
using WolfAI.Core.Domain.Messages;

namespace WolfAI.Tests.Domain.Messages;

public class MessageContentTests
{
    [Fact]
    public void MessageContent_Can_Be_Simple_Text()
    {
        var content = new MessageContent("hello");

        content.Text.Should().Be("hello");
        content.Items.Should().BeNull();
        content.IsSimple.Should().BeTrue();
        content.IsComplex.Should().BeFalse();
    }

    [Fact]
    public void MessageContent_Can_Be_Complex_Items()
    {
        var items = new MessageContentComplex[]
        {
            new MessageContentText("hi"),
            new MessageContentImageUrl("https://example.com/img.png", ImageDetail.High)
        };

        var content = new MessageContent(items);

        content.Text.Should().BeNull();
        content.Items.Should().NotBeNull();
        content.Items.Should().BeOfType<ReadOnlyCollection<MessageContentComplex>>();
        content.Items!.Count.Should().Be(2);
        content.IsSimple.Should().BeFalse();
        content.IsComplex.Should().BeTrue();
    }

    [Fact]
    public void MessageContent_Throws_When_Text_Null()
    {
        var act = () => new MessageContent((string)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MessageContent_Throws_When_Items_Null()
    {
        var act = () => new MessageContent((IReadOnlyList<MessageContentComplex>)null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
