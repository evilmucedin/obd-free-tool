namespace ObdFree.Core.Tests.Transport;

public class FakeObdTransportTests
{
    [Fact]
    public async Task SendCommand_ReturnsCannedResponse()
    {
        var transport = new FakeObdTransport(new Dictionary<string, string>
        {
            ["010C"] = "410C0CF8",
        });

        await transport.OpenAsync();
        string response = await transport.SendCommandAsync("010C");

        Assert.True(transport.IsOpen);
        Assert.Equal("410C0CF8", response);
        Assert.Equal(["010C"], transport.SentCommands);
    }

    [Fact]
    public async Task SendCommand_UnknownCommand_ReturnsNoData()
    {
        var transport = new FakeObdTransport(new Dictionary<string, string>());
        await transport.OpenAsync();

        Assert.Equal("NO DATA", await transport.SendCommandAsync("ABCD"));
    }

    [Fact]
    public async Task SendCommand_WhenClosed_Throws()
    {
        var transport = new FakeObdTransport(new Dictionary<string, string>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => transport.SendCommandAsync("010C"));
    }
}
