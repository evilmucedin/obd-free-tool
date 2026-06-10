using System.Text;
using ObdFree.Core.Protocol;
using ObdFree.Core.VehicleInfo;

namespace ObdFree.Core.Tests.VehicleInfo;

public class VinDecoderTests
{
    [Fact]
    public void Decode_ExtractsVinFromMode09Response()
    {
        const string vin = "1HGCM82633A004352"; // 17 chars
        byte[] data = BuildResponse(vin);

        Assert.Equal(vin, VinDecoder.Decode(data));
    }

    [Fact]
    public void Decode_NoVinResponse_ReturnsNull()
    {
        Assert.Null(VinDecoder.Decode([0x41, 0x00, 0x01]));
    }

    [Fact]
    public void Decode_TooShort_ReturnsNull()
    {
        // 49 02 01 then only a few ASCII chars.
        byte[] data = [0x49, 0x02, 0x01, .. Encoding.ASCII.GetBytes("ABC")];

        Assert.Null(VinDecoder.Decode(data));
    }

    [Fact]
    public void Decode_ToleratesRealWorldHexString()
    {
        // Build the typical "49 02 01 <17 ascii>" then round-trip via hex text.
        byte[] data = BuildResponse("JTDKARFU1J3059999");
        string hex = HexBytes.ToHex(data);

        Assert.Equal("JTDKARFU1J3059999", VinDecoder.Decode(HexBytes.Parse(hex)));
    }

    private static byte[] BuildResponse(string vin)
    {
        var bytes = new List<byte> { 0x49, 0x02, 0x01 };
        bytes.AddRange(Encoding.ASCII.GetBytes(vin));
        return [.. bytes];
    }
}
