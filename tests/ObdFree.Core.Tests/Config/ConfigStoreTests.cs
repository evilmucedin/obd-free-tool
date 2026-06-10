using ObdFree.Core.Config;
using ObdFree.Core.Modes;
using ObdFree.Core.Transport;

namespace ObdFree.Core.Tests.Config;

public class ConfigStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"obd-cfg-{Guid.NewGuid():N}.json");

    [Fact]
    public void Load_MissingFile_ReturnsSafeDefault()
    {
        var store = new ConfigStore(_path);

        Assert.Equal(OperatingMode.Safe, store.Load().Mode);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsMode()
    {
        var store = new ConfigStore(_path);

        store.Save(new AppConfig { Mode = OperatingMode.Professional });

        Assert.Equal(OperatingMode.Professional, new ConfigStore(_path).Load().Mode);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllSettings()
    {
        var store = new ConfigStore(_path);
        store.Save(new AppConfig
        {
            Mode = OperatingMode.Professional,
            ConnectionKind = ConnectionKind.WiFi,
            Target = "192.168.4.1:35000",
            BaudRate = 115200,
            VehicleProfileKey = "toyota",
            AdapterProfileKey = "launch",
        });

        AppConfig loaded = new ConfigStore(_path).Load();

        Assert.Equal(OperatingMode.Professional, loaded.Mode);
        Assert.Equal(ConnectionKind.WiFi, loaded.ConnectionKind);
        Assert.Equal("192.168.4.1:35000", loaded.Target);
        Assert.Equal(115200, loaded.BaudRate);
        Assert.Equal("toyota", loaded.VehicleProfileKey);
        Assert.Equal("launch", loaded.AdapterProfileKey);
    }

    [Fact]
    public void Save_WritesHumanReadableEnum()
    {
        new ConfigStore(_path).Save(new AppConfig { Mode = OperatingMode.Professional });

        Assert.Contains("Professional", File.ReadAllText(_path), StringComparison.Ordinal);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefault()
    {
        File.WriteAllText(_path, "{ not valid json");

        Assert.Equal(OperatingMode.Safe, new ConfigStore(_path).Load().Mode);
    }

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }

        GC.SuppressFinalize(this);
    }
}
