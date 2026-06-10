using ObdFree.Core.Tests.Transport;
using ObdFree.Core.Uds;

namespace ObdFree.Core.Tests.Uds;

public class UdsClientTests
{
    [Fact]
    public async Task ConfigureAsync_AddressesTheModule()
    {
        var transport = new FakeObdTransport(new Dictionary<string, string>());
        await transport.OpenAsync();
        var client = new UdsClient(transport);

        await client.ConfigureAsync(ToyotaModules.Srs);

        Assert.Contains("ATSP6", transport.SentCommands);          // CAN 11/500
        Assert.Contains("ATSH7B0", transport.SentCommands);        // request header
        Assert.Contains("ATCRA7B8", transport.SentCommands);       // response filter
        Assert.Contains("ATFCSH7B0", transport.SentCommands);      // flow-control header
    }

    [Fact]
    public async Task ReadDtcsAsync_ParsesModuleCodes()
    {
        var transport = new FakeObdTransport(new Dictionary<string, string>
        {
            ["1902FF"] = "5902FF8100130A\r>",
        });
        await transport.OpenAsync();
        var client = new UdsClient(transport);

        var codes = await client.ReadDtcsAsync();

        Assert.Equal(["B0100-13"], codes.Select(c => c.ToString()));
    }

    [Fact]
    public async Task ClearDtcsAsync_PositiveResponse_ReturnsTrue()
    {
        var transport = new FakeObdTransport(new Dictionary<string, string>
        {
            ["14FFFFFF"] = "54\r>",
        });
        await transport.OpenAsync();
        var client = new UdsClient(transport);

        Assert.True(await client.ClearDtcsAsync());
        Assert.Contains("14FFFFFF", transport.SentCommands);
    }

    [Fact]
    public async Task ClearDtcsAsync_Error_ReturnsFalse()
    {
        var transport = new FakeObdTransport(new Dictionary<string, string>
        {
            ["14FFFFFF"] = "CAN ERROR\r>",
        });
        await transport.OpenAsync();
        var client = new UdsClient(transport);

        Assert.False(await client.ClearDtcsAsync());
    }

    [Fact]
    public void EcuModule_WithHeaders_OverridesAndKeeps()
    {
        EcuModule overridden = ToyotaModules.Srs.WithHeaders("750", null);

        Assert.Equal("750", overridden.RequestHeader);
        Assert.Equal("7B8", overridden.ResponseHeader); // unchanged
    }
}
