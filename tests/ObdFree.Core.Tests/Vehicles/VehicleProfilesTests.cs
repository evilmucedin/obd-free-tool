using ObdFree.Core.Vehicles;

namespace ObdFree.Core.Tests.Vehicles;

public class VehicleProfilesTests
{
    [Theory]
    [InlineData("toyota")]
    [InlineData("Toyota")]
    [InlineData("lexus")]
    public void TryGet_ResolvesToyotaAndLexusToCan(string key)
    {
        Assert.True(VehicleProfiles.TryGet(key, out VehicleProfile? profile));
        Assert.NotNull(profile);
        Assert.Equal(ObdProtocol.Can11Bit500k, profile!.PreferredProtocol);
    }

    [Fact]
    public void Generic_UsesAutoDetect()
    {
        Assert.Equal(ObdProtocol.Auto, VehicleProfiles.Generic.PreferredProtocol);
    }

    [Fact]
    public void TryGet_UnknownReturnsFalse()
    {
        Assert.False(VehicleProfiles.TryGet("delorean", out VehicleProfile? profile));
        Assert.Null(profile);
    }

    [Fact]
    public void All_ContainsTheKnownProfiles()
    {
        var keys = VehicleProfiles.All.Select(p => p.Key).ToHashSet();
        Assert.Contains("generic", keys);
        Assert.Contains("toyota", keys);
        Assert.Contains("lexus", keys);
    }
}
