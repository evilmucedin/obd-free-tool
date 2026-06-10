using ObdFree.Core.Protocol;

namespace ObdFree.Core.Tests.Protocol;

public class HexBytesTests
{
    [Theory]
    [InlineData("410C0CE4", new byte[] { 0x41, 0x0C, 0x0C, 0xE4 })]
    [InlineData("41 0C 0C E4", new byte[] { 0x41, 0x0C, 0x0C, 0xE4 })]
    [InlineData("", new byte[0])]
    [InlineData("43", new byte[] { 0x43 })]
    public void Parse_HandlesSpacingAndCase(string text, byte[] expected)
    {
        Assert.Equal(expected, HexBytes.Parse(text));
    }

    [Fact]
    public void Parse_IgnoresNonHexNoise()
    {
        Assert.Equal(new byte[] { 0x41, 0x0C }, HexBytes.Parse("41\r0C\r"));
    }

    [Fact]
    public void Parse_OddDigits_Throws()
    {
        Assert.Throws<FormatException>(() => HexBytes.Parse("410C0"));
    }

    [Fact]
    public void ToHex_RoundTrips()
    {
        Assert.Equal("410C0CE4", HexBytes.ToHex([0x41, 0x0C, 0x0C, 0xE4]));
    }
}
