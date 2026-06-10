using ObdFree.Core.Protocol;

namespace ObdFree.Core.Tests.Protocol;

public class ObdPayloadTests
{
    [Fact]
    public void TryGetParameter_FindsPayloadAfterEchoedPid()
    {
        byte[] data = [0x41, 0x0C, 0x0C, 0xE4];

        bool found = ObdPayload.TryGetParameter(data, 0x01, 0x0C, out byte[] payload);

        Assert.True(found);
        Assert.Equal(new byte[] { 0x0C, 0xE4 }, payload);
    }

    [Fact]
    public void TryGetParameter_WrongPid_NotFound()
    {
        byte[] data = [0x41, 0x0C, 0x0C, 0xE4];

        Assert.False(ObdPayload.TryGetParameter(data, 0x01, 0x0D, out byte[] payload));
        Assert.Empty(payload);
    }

    [Fact]
    public void TryGetParameter_EmptyData_NotFound()
    {
        Assert.False(ObdPayload.TryGetParameter([], 0x01, 0x0C, out _));
    }
}
