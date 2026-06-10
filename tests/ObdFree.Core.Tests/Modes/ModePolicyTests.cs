using ObdFree.Core.Modes;

namespace ObdFree.Core.Tests.Modes;

public class ModePolicyTests
{
    [Theory]
    [InlineData(AppFeature.ReadStatus)]
    [InlineData(AppFeature.ReadReadiness)]
    [InlineData(AppFeature.ReadVin)]
    [InlineData(AppFeature.ReadDtc)]
    public void Safe_AllowsReadOnlyFeatures(AppFeature feature)
    {
        Assert.True(ModePolicy.IsAllowed(OperatingMode.Safe, feature));
        Assert.False(ModePolicy.RequiresProfessional(feature));
        Assert.Equal(OperatingMode.Safe, ModePolicy.RequiredMode(feature));
    }

    [Theory]
    [InlineData(AppFeature.ClearDtc)]
    [InlineData(AppFeature.SrsRead)]
    [InlineData(AppFeature.SrsClear)]
    public void Safe_BlocksDangerousFeatures(AppFeature feature)
    {
        Assert.False(ModePolicy.IsAllowed(OperatingMode.Safe, feature));
        Assert.True(ModePolicy.RequiresProfessional(feature));
        Assert.Equal(OperatingMode.Professional, ModePolicy.RequiredMode(feature));
    }

    [Theory]
    [InlineData(AppFeature.ReadStatus)]
    [InlineData(AppFeature.ClearDtc)]
    [InlineData(AppFeature.SrsClear)]
    public void Professional_AllowsEverything(AppFeature feature)
    {
        Assert.True(ModePolicy.IsAllowed(OperatingMode.Professional, feature));
    }

    [Fact]
    public void Exception_CarriesFeatureAndMode()
    {
        var ex = new FeatureNotAllowedInModeException(AppFeature.SrsClear, OperatingMode.Safe);

        Assert.Equal(AppFeature.SrsClear, ex.Feature);
        Assert.Equal(OperatingMode.Safe, ex.CurrentMode);
        Assert.Contains("Professional", ex.Message, StringComparison.Ordinal);
    }
}
