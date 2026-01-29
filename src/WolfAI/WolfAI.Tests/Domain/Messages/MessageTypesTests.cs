using FluentAssertions;
using WolfAI.Core.Domain.Messages;

namespace WolfAI.Tests.Domain.Messages;

public class MessageTypesTests
{
    [Fact]
    public void MessageType_Has_Expected_Values()
    {
        MessageType.Human.Should().Be((MessageType)0);
        MessageType.AI.Should().Be((MessageType)1);
        MessageType.Tool.Should().Be((MessageType)2);
        MessageType.System.Should().Be((MessageType)3);
        MessageType.Function.Should().Be((MessageType)4);
        MessageType.Remove.Should().Be((MessageType)5);
    }

    [Fact]
    public void ImageDetail_Has_Expected_Values()
    {
        ImageDetail.Auto.Should().Be((ImageDetail)0);
        ImageDetail.Low.Should().Be((ImageDetail)1);
        ImageDetail.High.Should().Be((ImageDetail)2);
    }
}
