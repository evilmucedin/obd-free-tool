using ObdFree.Core.Diagnostics;

namespace ObdFree.Core.Tests.Diagnostics;

public class DtcParserTests
{
    [Fact]
    public void Parse_NonCan_TwoCodes()
    {
        // 43 then pairs (even remaining length, no count byte).
        byte[] data = [0x43, 0x01, 0x33, 0x04, 0x20];

        var codes = DtcParser.Parse(data, DtcParser.StoredResponseByte);

        Assert.Equal(["P0133", "P0420"], codes.Select(c => c.Code));
    }

    [Fact]
    public void Parse_Can_WithCountByte()
    {
        // 43 02 then two DTCs (odd remaining length -> count byte dropped).
        byte[] data = [0x43, 0x02, 0x01, 0x33, 0x04, 0x20];

        var codes = DtcParser.Parse(data, DtcParser.StoredResponseByte);

        Assert.Equal(["P0133", "P0420"], codes.Select(c => c.Code));
    }

    [Fact]
    public void Parse_SkipsZeroPadding()
    {
        byte[] data = [0x43, 0x01, 0x33, 0x00, 0x00];

        var codes = DtcParser.Parse(data, DtcParser.StoredResponseByte);

        Assert.Equal(["P0133"], codes.Select(c => c.Code));
    }

    [Fact]
    public void Parse_NoCodes_ReturnsEmpty()
    {
        byte[] data = [0x43, 0x00, 0x00];

        Assert.Empty(DtcParser.Parse(data, DtcParser.StoredResponseByte));
    }

    [Fact]
    public void Parse_Pending_UsesMode07ResponseByte()
    {
        byte[] data = [0x47, 0x01, 0x33];

        var codes = DtcParser.Parse(data, DtcParser.PendingResponseByte);

        Assert.Equal(["P0133"], codes.Select(c => c.Code));
    }

    [Fact]
    public void Parse_MissingResponseByte_ReturnsEmpty()
    {
        Assert.Empty(DtcParser.Parse([0x00, 0x01], DtcParser.StoredResponseByte));
    }
}
