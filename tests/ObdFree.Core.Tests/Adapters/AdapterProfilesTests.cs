using ObdFree.Core.Adapters;

namespace ObdFree.Core.Tests.Adapters;

public class AdapterProfilesTests
{
    [Fact]
    public void Standard_UsesAtzAndNoDelays()
    {
        Assert.Equal("ATZ", AdapterProfiles.Standard.ResetCommand);
        Assert.Equal(0, AdapterProfiles.Standard.ResetDelayMs);
        Assert.Equal(0, AdapterProfiles.Standard.InterCommandDelayMs);
    }

    [Fact]
    public void Launch_UsesWarmStartAndTolerantTiming()
    {
        Assert.Equal("ATWS", AdapterProfiles.Launch.ResetCommand);
        Assert.True(AdapterProfiles.Launch.ResetDelayMs > 0);
        Assert.True(AdapterProfiles.Launch.InterCommandDelayMs > 0);
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("Launch")]
    [InlineData("LAUNCH")]
    public void TryGet_IsCaseInsensitive(string key)
    {
        Assert.True(AdapterProfiles.TryGet(key, out AdapterProfile? profile));
        Assert.NotNull(profile);
    }

    [Fact]
    public void TryGet_UnknownReturnsFalse()
    {
        Assert.False(AdapterProfiles.TryGet("nope", out AdapterProfile? profile));
        Assert.Null(profile);
    }
}
