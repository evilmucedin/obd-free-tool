using ObdFree.Core.Uds;

namespace ObdFree.Core.Tests.Uds;

public class UdsResponseTests
{
    [Fact]
    public void Parse_SingleFrame()
    {
        UdsResponse response = ObdResponseFor("5902FF8100130A\r>");

        Assert.Equal(UdsResponseStatus.Ok, response.Status);
        Assert.Equal(new byte[] { 0x59, 0x02, 0xFF, 0x81, 0x00, 0x13, 0x0A }, response.Data);
    }

    [Fact]
    public void Parse_MultiFrame_StripsIndexPrefixesAndLengthLine()
    {
        // Typical ELM multi-frame output: a length line, then "0:" / "1:" frames.
        UdsResponse response = ObdResponseFor("014\r0:5902FF810013\r1:0A8100140B\r>");

        Assert.Equal(
            new byte[] { 0x59, 0x02, 0xFF, 0x81, 0x00, 0x13, 0x0A, 0x81, 0x00, 0x14, 0x0B },
            response.Data);
    }

    [Theory]
    [InlineData("NO DATA\r>", UdsResponseStatus.NoData)]
    [InlineData("CAN ERROR\r>", UdsResponseStatus.Error)]
    [InlineData("?\r>", UdsResponseStatus.Error)]
    public void Parse_StatusClassification(string raw, UdsResponseStatus expected)
    {
        Assert.Equal(expected, ObdResponseFor(raw).Status);
    }

    [Fact]
    public void TryGetPositive_FindsServiceByte()
    {
        UdsResponse response = ObdResponseFor("5902FF810013 0A\r>");

        Assert.True(response.TryGetPositive(0x59, out byte[] payload));
        Assert.Equal(new byte[] { 0x02, 0xFF, 0x81, 0x00, 0x13, 0x0A }, payload);
    }

    private static UdsResponse ObdResponseFor(string raw) => UdsResponse.Parse(raw);
}
