using ObdFree.Core.Diagnostics;

namespace ObdFree.Core.Tests.Diagnostics;

public class DiagnosticTroubleCodeTests
{
    [Theory]
    [InlineData(0x01, 0x33, "P0133")] // Powertrain
    [InlineData(0x00, 0x00, "P0000")]
    [InlineData(0x42, 0x0A, "C020A")] // Chassis
    [InlineData(0x84, 0x55, "B0455")] // Body
    [InlineData(0xC1, 0x23, "U0123")] // Network
    [InlineData(0xFF, 0xFF, "U3FFF")]
    public void Decode_ProducesCanonicalCode(byte a, byte b, string expected)
    {
        DiagnosticTroubleCode dtc = DiagnosticTroubleCode.Decode(a, b);

        Assert.Equal(expected, dtc.Code);
        Assert.Equal(expected, dtc.ToString());
    }
}
