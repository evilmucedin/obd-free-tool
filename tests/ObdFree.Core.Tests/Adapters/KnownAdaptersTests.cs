using ObdFree.Core.Adapters;
using ObdFree.Core.Transport;

namespace ObdFree.Core.Tests.Adapters;

public class KnownAdaptersTests
{
    [Fact]
    public void Catalog_HasPopularDongles()
    {
        var keys = KnownAdapters.All.Select(a => a.Key).ToHashSet();
        Assert.Contains("bafx-bt", keys);
        Assert.Contains("veepeak-wifi", keys);
        Assert.Contains("veepeak-ble", keys);
        Assert.Contains("obdlink-cx", keys);
        Assert.Contains("generic-usb", keys);
    }

    [Fact]
    public void Keys_AreUnique()
    {
        var keys = KnownAdapters.All.Select(a => a.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Ble_DonglesAreNotSupported()
    {
        Assert.True(KnownAdapters.TryGet("veepeak-ble", out KnownAdapter? ble));
        Assert.False(ble!.IsSupported);

        // Supported list excludes BLE-only dongles.
        Assert.DoesNotContain(KnownAdapters.Supported, a => a.Link == DongleLink.BluetoothLe);
        Assert.All(KnownAdapters.Supported, a => Assert.True(a.IsSupported));
    }

    [Fact]
    public void WiFiDongle_CreatesConnectionWithKnownEndpoint()
    {
        Assert.True(KnownAdapters.TryGet("veepeak-wifi", out KnownAdapter? veepeak));

        ObdConnection connection = veepeak!.CreateConnection();

        Assert.Equal(ConnectionKind.WiFi, connection.Kind);
        Assert.Equal("192.168.0.10:35000", connection.Target);
    }

    [Fact]
    public void SerialDongle_RequiresPort()
    {
        Assert.True(KnownAdapters.TryGet("bafx-bt", out KnownAdapter? bafx));
        Assert.True(bafx!.RequiresSerialPort);

        Assert.Throws<InvalidOperationException>(() => bafx.CreateConnection(null));

        ObdConnection connection = bafx.CreateConnection("/dev/rfcomm0");
        Assert.Equal(ConnectionKind.Bluetooth, connection.Kind);
        Assert.Equal("/dev/rfcomm0", connection.Target);
    }

    [Fact]
    public void BleDongle_CreateConnectionThrows()
    {
        Assert.True(KnownAdapters.TryGet("obdlink-cx", out KnownAdapter? cx));
        Assert.Throws<InvalidOperationException>(() => cx!.CreateConnection("/dev/x"));
    }

    [Fact]
    public void RecommendedAdapterProfile_Resolves()
    {
        Assert.True(KnownAdapters.TryGet("generic-usb", out KnownAdapter? generic));
        Assert.Equal(AdapterProfiles.Launch, generic!.AdapterProfile);

        Assert.True(KnownAdapters.TryGet("obdlink-lx", out KnownAdapter? lx));
        Assert.Equal(AdapterProfiles.Standard, lx!.AdapterProfile);
    }
}
