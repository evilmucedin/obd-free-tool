using ObdFree.Core.Config;
using ObdFree.Core.Modes;

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
