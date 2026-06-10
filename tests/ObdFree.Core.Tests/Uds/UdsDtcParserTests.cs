using ObdFree.Core.Uds;

namespace ObdFree.Core.Tests.Uds;

public class UdsDtcParserTests
{
    [Fact]
    public void ParseDtcs_DecodesBodyCodesWithFailureType()
    {
        // 59 02 <mask> then two DTCs: 81 00 13 (status 0A) and 81 00 14 (status 08)
        UdsResponse response = UdsResponse.Parse("5902FF8100130A810014 08\r>");

        var codes = UdsDtcParser.ParseDtcs(response);

        Assert.Equal(["B0100-13", "B0100-14"], codes.Select(c => c.ToString()));
        Assert.Equal(0x0A, codes[0].Status);
    }

    [Fact]
    public void ParseDtcs_SkipsZeroPadding()
    {
        UdsResponse response = UdsResponse.Parse("5902FF000000 00\r>");

        Assert.Empty(UdsDtcParser.ParseDtcs(response));
    }

    [Fact]
    public void ParseDtcs_NoData_ReturnsEmpty()
    {
        Assert.Empty(UdsDtcParser.ParseDtcs(UdsResponse.Parse("NO DATA\r>")));
    }

    [Fact]
    public void IsActive_ReflectsTestFailedBit()
    {
        var failed = UdsDtc.Decode(0x81, 0x00, 0x13, 0x09); // bit0 set
        var passed = UdsDtc.Decode(0x81, 0x00, 0x13, 0x08); // bit0 clear

        Assert.True(failed.IsActive);
        Assert.False(passed.IsActive);
    }

    [Fact]
    public void TryParseCount_ReadsNumberOfDtcs()
    {
        // 59 01 <mask> <formatId> <countHi> <countLo>
        UdsResponse response = UdsResponse.Parse("5901FF010003\r>");

        Assert.True(UdsDtcParser.TryParseCount(response, out int count));
        Assert.Equal(3, count);
    }
}
