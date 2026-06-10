using ObdFree.Core.Pids;

namespace ObdFree.Core.Tests.Pids;

public class PidDecodersTests
{
    [Theory]
    [InlineData(0x0C, 0xE4, 825.0)] // classic ELM327 sample: 0x0CE4 = 3300 -> /4
    [InlineData(0x00, 0x00, 0.0)]
    [InlineData(0xFF, 0xFF, 16383.75)]
    public void EngineRpm_DecodesCorrectly(byte a, byte b, double expected)
    {
        PidValue value = PidDecoders.EngineRpm(a, b);

        Assert.Equal(expected, value.Value, precision: 2);
        Assert.Equal("rpm", value.Unit);
    }

    [Theory]
    [InlineData(0x00, 0)]
    [InlineData(0x64, 100)]
    [InlineData(0xFF, 255)]
    public void VehicleSpeed_DecodesCorrectly(byte a, int expected)
    {
        PidValue value = PidDecoders.VehicleSpeed(a);

        Assert.Equal(expected, value.Value);
        Assert.Equal("km/h", value.Unit);
    }

    [Theory]
    [InlineData(0x28, -0.0)] // 40 -> 0 C
    [InlineData(0x00, -40.0)]
    [InlineData(0xFF, 215.0)]
    public void CoolantTemperature_DecodesCorrectly(byte a, double expected)
    {
        PidValue value = PidDecoders.CoolantTemperature(a);

        Assert.Equal(expected, value.Value, precision: 2);
    }

    [Theory]
    [InlineData(0x00, 0.0)]
    [InlineData(0xFF, 100.0)]
    public void EngineLoad_DecodesAsPercentage(byte a, double expected)
    {
        PidValue value = PidDecoders.EngineLoad(a);

        Assert.Equal(expected, value.Value, precision: 2);
        Assert.Equal("%", value.Unit);
    }
}
