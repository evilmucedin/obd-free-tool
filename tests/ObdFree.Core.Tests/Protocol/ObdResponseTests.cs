using ObdFree.Core.Protocol;

namespace ObdFree.Core.Tests.Protocol;

public class ObdResponseTests
{
    [Fact]
    public void Parse_StripsSearchingAndPrompt()
    {
        ObdResponse response = ObdResponse.Parse("SEARCHING...\r410C0CE4\r\r>");

        Assert.Equal(ObdResponseStatus.Ok, response.Status);
        Assert.True(response.IsSuccess);
        Assert.Equal(new byte[] { 0x41, 0x0C, 0x0C, 0xE4 }, response.Data);
        Assert.Single(response.DataLines);
    }

    [Fact]
    public void Parse_NoData_IsNotSuccess()
    {
        ObdResponse response = ObdResponse.Parse("NO DATA\r\r>");

        Assert.Equal(ObdResponseStatus.NoData, response.Status);
        Assert.False(response.IsSuccess);
        Assert.Empty(response.Data);
    }

    [Theory]
    [InlineData("?\r>")]
    [InlineData("UNABLE TO CONNECT\r>")]
    [InlineData("CAN ERROR\r>")]
    public void Parse_ErrorResponses(string raw)
    {
        ObdResponse response = ObdResponse.Parse(raw);

        Assert.Equal(ObdResponseStatus.Error, response.Status);
        Assert.False(response.IsSuccess);
    }

    [Fact]
    public void Parse_MultiLineConcatenatesData()
    {
        ObdResponse response = ObdResponse.Parse("4302\r0133\r0420\r>");

        Assert.Equal(new byte[] { 0x43, 0x02, 0x01, 0x33, 0x04, 0x20 }, response.Data);
    }
}
