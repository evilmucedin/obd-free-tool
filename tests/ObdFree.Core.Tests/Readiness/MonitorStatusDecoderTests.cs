using ObdFree.Core.Readiness;

namespace ObdFree.Core.Tests.Readiness;

public class MonitorStatusDecoderTests
{
    [Fact]
    public void Decode_MilOnAndDtcCount()
    {
        // A = 0x83 => MIL on, 3 DTCs.
        MonitorStatus status = MonitorStatusDecoder.Decode(0x83, 0x00, 0x00, 0x00);

        Assert.True(status.MilOn);
        Assert.Equal(3, status.DtcCount);
    }

    [Fact]
    public void Decode_MilOffNoDtcs()
    {
        MonitorStatus status = MonitorStatusDecoder.Decode(0x00, 0x00, 0x00, 0x00);

        Assert.False(status.MilOn);
        Assert.Equal(0, status.DtcCount);
    }

    [Fact]
    public void Decode_ContinuousMonitors_SupportedAndReady()
    {
        // B: bits0-2 supported (0x07), bits4-6 = 0 => all complete/ready.
        MonitorStatus status = MonitorStatusDecoder.Decode(0x00, 0x07, 0x00, 0x00);

        var misfire = status.Monitors.First(m => m.Name == "Misfire");
        Assert.True(misfire.Supported);
        Assert.True(misfire.Complete);
        Assert.Equal(0, status.NotReadyCount);
    }

    [Fact]
    public void Decode_ContinuousMonitor_NotReady()
    {
        // B: misfire supported (bit0) + incomplete (bit4).
        MonitorStatus status = MonitorStatusDecoder.Decode(0x00, 0x11, 0x00, 0x00);

        var misfire = status.Monitors.First(m => m.Name == "Misfire");
        Assert.True(misfire.Supported);
        Assert.False(misfire.Complete);
        Assert.Equal(1, status.NotReadyCount);
    }

    [Fact]
    public void Decode_SparkIgnition_NamesCatalyst()
    {
        // C bit0 = Catalyst supported; D bit0 = 0 => ready. B bit3 clear => spark.
        MonitorStatus status = MonitorStatusDecoder.Decode(0x00, 0x00, 0x01, 0x00);

        Assert.False(status.IsCompressionIgnition);
        var catalyst = status.Monitors.First(m => m.Name == "Catalyst");
        Assert.True(catalyst.Supported);
        Assert.True(catalyst.Complete);
    }

    [Fact]
    public void Decode_CompressionIgnition_UsesDieselNames()
    {
        // B bit3 set => compression. C bit0 supported => NMHC Catalyst.
        MonitorStatus status = MonitorStatusDecoder.Decode(0x00, 0x08, 0x01, 0x01);

        Assert.True(status.IsCompressionIgnition);
        var nmhc = status.Monitors.First(m => m.Name == "NMHC Catalyst");
        Assert.True(nmhc.Supported);
        Assert.False(nmhc.Complete); // D bit0 set => not ready
    }

    [Fact]
    public void LikelyReadyForInspection_RequiresMilOffAndFewIncomplete()
    {
        // MIL off, one monitor not ready -> likely ready.
        MonitorStatus ok = MonitorStatusDecoder.Decode(0x00, 0x11, 0x00, 0x00);
        Assert.True(ok.LikelyReadyForInspection);

        // MIL on -> not ready regardless.
        MonitorStatus milOn = MonitorStatusDecoder.Decode(0x80, 0x00, 0x00, 0x00);
        Assert.False(milOn.LikelyReadyForInspection);

        // Two monitors not ready -> not ready.
        MonitorStatus twoIncomplete = MonitorStatusDecoder.Decode(0x00, 0x33, 0x00, 0x00);
        Assert.Equal(2, twoIncomplete.NotReadyCount);
        Assert.False(twoIncomplete.LikelyReadyForInspection);
    }
}
