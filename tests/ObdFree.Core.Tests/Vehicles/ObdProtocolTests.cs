using ObdFree.Core.Vehicles;

namespace ObdFree.Core.Tests.Vehicles;

public class ObdProtocolTests
{
    [Theory]
    [InlineData(ObdProtocol.Auto, "ATSP0")]
    [InlineData(ObdProtocol.Can11Bit500k, "ATSP6")]
    [InlineData(ObdProtocol.Can29Bit500k, "ATSP7")]
    [InlineData(ObdProtocol.Iso9141, "ATSP3")]
    public void ToSetProtocolCommand_MapsToAtsp(ObdProtocol protocol, string expected)
    {
        Assert.Equal(expected, protocol.ToSetProtocolCommand());
    }

    [Theory]
    [InlineData("auto", ObdProtocol.Auto)]
    [InlineData("CAN", ObdProtocol.Can11Bit500k)]
    [InlineData("can11", ObdProtocol.Can11Bit500k)]
    [InlineData("6", ObdProtocol.Can11Bit500k)]
    [InlineData("can29", ObdProtocol.Can29Bit500k)]
    [InlineData("iso9141", ObdProtocol.Iso9141)]
    [InlineData("kwp", ObdProtocol.Kwp2000FastInit)]
    public void TryParse_RecognizesNamesAndDigits(string text, ObdProtocol expected)
    {
        Assert.True(ObdProtocolExtensions.TryParse(text, out ObdProtocol protocol));
        Assert.Equal(expected, protocol);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nonsense")]
    public void TryParse_RejectsUnknown(string text)
    {
        Assert.False(ObdProtocolExtensions.TryParse(text, out _));
    }
}
