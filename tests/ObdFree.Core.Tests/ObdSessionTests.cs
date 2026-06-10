using ObdFree.Core;
using ObdFree.Core.Adapters;
using ObdFree.Core.Diagnostics;
using ObdFree.Core.Tests.Transport;
using ObdFree.Core.Vehicles;

namespace ObdFree.Core.Tests;

public class ObdSessionTests
{
    private static FakeObdTransport BuildTransport(Dictionary<string, string>? extra = null)
    {
        var responses = new Dictionary<string, string>
        {
            ["ATZ"] = "ELM327 v1.5\r\r>",
            ["ATE0"] = "OK\r>",
            ["ATL0"] = "OK\r>",
            ["ATS0"] = "OK\r>",
            ["ATH0"] = "OK\r>",
            ["ATSP0"] = "OK\r>",
            ["ATI"] = "ELM327 v1.5\r>",
            ["ATRV"] = "12.4V\r>",
        };

        if (extra is not null)
        {
            foreach (var kvp in extra)
            {
                responses[kvp.Key] = kvp.Value;
            }
        }

        return new FakeObdTransport(responses);
    }

    [Fact]
    public async Task ConnectAsync_RunsInitSequence_AndReturnsIdentity()
    {
        var transport = BuildTransport();
        await using var session = new ObdSession(transport);

        string identity = await session.ConnectAsync();

        Assert.Equal("ELM327 v1.5", identity);
        Assert.Equal(
            ["ATZ", "ATE0", "ATL0", "ATS0", "ATH0", "ATSP0", "ATI"],
            transport.SentCommands);
    }

    [Fact]
    public async Task ConnectAsync_ToyotaProfile_SetsCanProtocol()
    {
        var transport = BuildTransport();
        await using var session = new ObdSession(transport, VehicleProfiles.Toyota);

        await session.ConnectAsync();

        Assert.Equal(VehicleProfiles.Toyota, session.Profile);
        Assert.Contains("ATSP6", transport.SentCommands); // ISO 15765-4 CAN 11/500
        Assert.DoesNotContain("ATSP0", transport.SentCommands);
    }

    [Fact]
    public async Task DefaultProfile_IsGenericAuto()
    {
        var transport = BuildTransport();
        await using var session = new ObdSession(transport);

        await session.ConnectAsync();

        Assert.Equal(VehicleProfiles.Generic, session.Profile);
        Assert.Contains("ATSP0", transport.SentCommands);
    }

    [Fact]
    public async Task ConnectAsync_UsesAdapterResetCommand()
    {
        var transport = BuildTransport();
        // Custom Launch-like profile with no delays so the test stays fast.
        var adapter = new AdapterProfile("test", "Test", "ATWS", 0, 0, "");
        await using var session = new ObdSession(transport, VehicleProfiles.Toyota, adapter);

        await session.ConnectAsync();

        Assert.Equal("ATWS", transport.SentCommands[0]);
        Assert.DoesNotContain("ATZ", transport.SentCommands);
        Assert.True(session.AdapterLooksElmCompatible);
    }

    [Fact]
    public async Task ConnectAsync_FlagsNonElmDevice()
    {
        var transport = BuildTransport(new Dictionary<string, string>
        {
            ["ATI"] = "DBSCAR\r>", // proprietary Launch-style response
        });
        await using var session = new ObdSession(transport);

        await session.ConnectAsync();

        Assert.False(session.AdapterLooksElmCompatible);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsRespondingParameters()
    {
        var transport = BuildTransport(new Dictionary<string, string>
        {
            ["010C"] = "410C0CE4\r>", // RPM = 825
            ["010D"] = "410D64\r>",   // speed = 100 km/h
        });
        await using var session = new ObdSession(transport);

        ObdStatus status = await session.GetStatusAsync();

        Assert.Equal("ELM327 v1.5", status.AdapterIdentity);
        Assert.Equal("12.4V", status.BatteryVoltage);

        var byName = status.Readings.ToDictionary(r => r.Definition.Name, r => r.Value);
        Assert.Equal(825.0, byName["Engine RPM"].Value, precision: 2);
        Assert.Equal(100.0, byName["Vehicle speed"].Value);
        Assert.DoesNotContain("Coolant temperature", byName.Keys); // returned NO DATA
    }

    [Fact]
    public async Task ReadStoredCodesAsync_ParsesCodes()
    {
        var transport = BuildTransport(new Dictionary<string, string>
        {
            ["03"] = "4302013304 20\r>",
        });
        await using var session = new ObdSession(transport);

        IReadOnlyList<DiagnosticTroubleCode> codes = await session.ReadStoredCodesAsync();

        Assert.Equal(["P0133", "P0420"], codes.Select(c => c.Code));
    }

    [Fact]
    public async Task ReadReadinessAsync_DecodesMilAndMonitors()
    {
        var transport = BuildTransport(new Dictionary<string, string>
        {
            // 41 01 A B C D : A=0x00 (MIL off, 0 DTCs), B=0x07 monitors ready
            ["0101"] = "410100070000\r>",
        });
        await using var session = new ObdSession(transport, VehicleProfiles.Toyota);

        var status = await session.ReadReadinessAsync();

        Assert.NotNull(status);
        Assert.False(status!.MilOn);
        Assert.Equal(0, status.DtcCount);
        Assert.True(status.LikelyReadyForInspection);
    }

    [Fact]
    public async Task ReadVinAsync_DecodesVin()
    {
        var transport = BuildTransport(new Dictionary<string, string>
        {
            // 49 02 01 + ASCII "1HGCM82633A004352"
            ["0902"] = "490201314847434D3832363333413030343335320A\r>",
        });
        await using var session = new ObdSession(transport);

        Assert.Equal("1HGCM82633A004352", await session.ReadVinAsync());
    }

    [Fact]
    public async Task ReadPermanentCodesAsync_ParsesMode0A()
    {
        var transport = BuildTransport(new Dictionary<string, string>
        {
            ["0A"] = "4A01 0133\r>",
        });
        await using var session = new ObdSession(transport);

        var codes = await session.ReadPermanentCodesAsync();

        Assert.Equal(["P0133"], codes.Select(c => c.Code));
    }

    [Fact]
    public async Task ReadStoredCodesAsync_NoData_ReturnsEmpty()
    {
        var transport = BuildTransport(new Dictionary<string, string>
        {
            ["03"] = "NO DATA\r>",
        });
        await using var session = new ObdSession(transport);

        Assert.Empty(await session.ReadStoredCodesAsync());
    }

    [Fact]
    public async Task ClearCodesAsync_PositiveResponse_ReturnsTrue()
    {
        var transport = BuildTransport(new Dictionary<string, string>
        {
            ["04"] = "44\r>",
        });
        await using var session = new ObdSession(transport);

        Assert.True(await session.ClearCodesAsync());
        Assert.Contains("04", transport.SentCommands);
    }

    [Fact]
    public async Task ClearCodesAsync_Error_ReturnsFalse()
    {
        var transport = BuildTransport(new Dictionary<string, string>
        {
            ["04"] = "CAN ERROR\r>",
        });
        await using var session = new ObdSession(transport);

        Assert.False(await session.ClearCodesAsync());
    }

    [Fact]
    public async Task ReadModuleStatusAsync_DefaultsToSrs_AndParsesCodes()
    {
        var transport = BuildTransport(new Dictionary<string, string>
        {
            ["1902FF"] = "5902FF8100130A\r>",
        });
        await using var session = new ObdSession(transport, VehicleProfiles.Toyota);

        ModuleStatus status = await session.ReadModuleStatusAsync();

        Assert.Equal("srs", status.Module.Key);
        Assert.True(status.HasFaults);
        Assert.Equal(["B0100-13"], status.Codes.Select(c => c.ToString()));
        Assert.Contains("ATSH7B0", transport.SentCommands);
    }

    [Fact]
    public async Task ClearModuleCodesAsync_Srs_ReturnsTrueOnAck()
    {
        var transport = BuildTransport(new Dictionary<string, string>
        {
            ["14FFFFFF"] = "54\r>",
        });
        await using var session = new ObdSession(transport, VehicleProfiles.Toyota);

        Assert.True(await session.ClearModuleCodesAsync());
        Assert.Contains("14FFFFFF", transport.SentCommands);
    }
}
