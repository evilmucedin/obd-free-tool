using ObdFree.Core.Adapters;

namespace ObdFree.Core.Tests.Adapters;

public class AdapterCompatibilityTests
{
    [Theory]
    [InlineData("ELM327 v1.5", true)]
    [InlineData("ELM327 v2.3", true)]
    [InlineData("STN1110", true)]
    [InlineData("OBDLink MX", true)]
    [InlineData("vLinker FS", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("DBSCAR", false)]          // Launch proprietary
    [InlineData("?", false)]
    [InlineData("Thinkdiag", false)]
    public void IsLikelyElm_DetectsElmAdapters(string? identity, bool expected)
    {
        Assert.Equal(expected, AdapterCompatibility.IsLikelyElm(identity));
    }
}
