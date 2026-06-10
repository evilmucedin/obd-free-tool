using ObdFree.Core.Diagnostics;
using ObdFree.Core.Pids;
using ObdFree.Core.Protocol;
using ObdFree.Core.Transport;
using ObdFree.Core.Vehicles;

namespace ObdFree.Core;

/// <summary>A single live parameter reading.</summary>
/// <param name="Definition">The parameter that was read.</param>
/// <param name="Value">The decoded value.</param>
public readonly record struct LiveReading(PidDefinition Definition, PidValue Value);

/// <summary>A snapshot of adapter and vehicle status.</summary>
/// <param name="AdapterIdentity">The adapter's self-reported identity (from <c>ATI</c>).</param>
/// <param name="BatteryVoltage">The measured battery voltage text (from <c>ATRV</c>), if available.</param>
/// <param name="Readings">Live parameter readings that responded.</param>
public sealed record ObdStatus(
    string AdapterIdentity,
    string? BatteryVoltage,
    IReadOnlyList<LiveReading> Readings);

/// <summary>
/// High-level OBD-II session over an <see cref="IObdTransport"/>. Initializes the
/// ELM327 adapter, then reads status / live data and reads or clears Diagnostic
/// Trouble Codes — the core ForScan-style workflow.
/// </summary>
public sealed class ObdSession(IObdTransport transport, VehicleProfile? profile = null) : IAsyncDisposable
{
    private readonly IObdTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    /// <summary>Gets the vehicle profile guiding this session (defaults to generic).</summary>
    public VehicleProfile Profile { get; } = profile ?? VehicleProfiles.Generic;

    private async Task EnsureOpenAsync(CancellationToken cancellationToken)
    {
        if (!_transport.IsOpen)
        {
            await _transport.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Opens the transport and runs the ELM327 initialization sequence
    /// (reset, echo/linefeed/spaces off, then sets the profile's preferred
    /// protocol — auto for generic, ISO 15765-4 CAN for Toyota/Lexus).
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The adapter identity reported by <c>ATI</c>.</returns>
    public async Task<string> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);

        await _transport.SendCommandAsync("ATZ", cancellationToken).ConfigureAwait(false);   // reset
        await _transport.SendCommandAsync("ATE0", cancellationToken).ConfigureAwait(false);  // echo off
        await _transport.SendCommandAsync("ATL0", cancellationToken).ConfigureAwait(false);  // linefeeds off
        await _transport.SendCommandAsync("ATS0", cancellationToken).ConfigureAwait(false);  // spaces off
        await _transport.SendCommandAsync("ATH0", cancellationToken).ConfigureAwait(false);  // headers off

        // Set the protocol for this vehicle (ATSP0 = auto, ATSP6 = CAN 11/500, …).
        await _transport.SendCommandAsync(Profile.PreferredProtocol.ToSetProtocolCommand(), cancellationToken)
            .ConfigureAwait(false);

        string identity = (await _transport.SendCommandAsync("ATI", cancellationToken).ConfigureAwait(false))
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal)
            .Trim();

        return string.IsNullOrWhiteSpace(identity) ? "ELM327 (unknown)" : identity;
    }

    /// <summary>Reads the adapter's measured battery voltage (<c>ATRV</c>).</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The voltage text (e.g. <c>12.4V</c>), or <see langword="null"/> if unavailable.</returns>
    public async Task<string?> ReadBatteryVoltageAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
        string raw = (await _transport.SendCommandAsync("ATRV", cancellationToken).ConfigureAwait(false))
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal)
            .Trim();

        return raw.Length == 0 ? null : raw;
    }

    /// <summary>Reads a single live parameter.</summary>
    /// <param name="definition">The parameter to read.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The decoded value, or <see langword="null"/> if the ECU returned no data.</returns>
    public async Task<PidValue?> ReadParameterAsync(PidDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
        string raw = await _transport.SendCommandAsync(definition.Command, cancellationToken).ConfigureAwait(false);
        ObdResponse response = ObdResponse.Parse(raw);
        if (!response.IsSuccess)
        {
            return null;
        }

        if (!ObdPayload.TryGetParameter(response.Data, definition.Mode, definition.Pid, out byte[] payload)
            || payload.Length < definition.DataBytes)
        {
            return null;
        }

        return definition.Decode(payload);
    }

    /// <summary>
    /// Reads a status snapshot: adapter identity, battery voltage, and every
    /// catalog parameter that responds.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The status snapshot.</returns>
    public async Task<ObdStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        string identity = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        string? voltage = await ReadBatteryVoltageAsync(cancellationToken).ConfigureAwait(false);

        var readings = new List<LiveReading>();
        foreach (PidDefinition definition in PidCatalog.All)
        {
            PidValue? value = await ReadParameterAsync(definition, cancellationToken).ConfigureAwait(false);
            if (value is { } v)
            {
                readings.Add(new LiveReading(definition, v));
            }
        }

        return new ObdStatus(identity, voltage, readings);
    }

    /// <summary>Reads stored trouble codes (Mode 03).</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The stored trouble codes.</returns>
    public Task<IReadOnlyList<DiagnosticTroubleCode>> ReadStoredCodesAsync(CancellationToken cancellationToken = default)
        => ReadCodesAsync("03", DtcParser.StoredResponseByte, cancellationToken);

    /// <summary>Reads pending trouble codes (Mode 07).</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The pending trouble codes.</returns>
    public Task<IReadOnlyList<DiagnosticTroubleCode>> ReadPendingCodesAsync(CancellationToken cancellationToken = default)
        => ReadCodesAsync("07", DtcParser.PendingResponseByte, cancellationToken);

    /// <summary>
    /// Clears stored trouble codes and the MIL ("check engine" light) — Mode 04.
    /// This is a write operation; callers must obtain explicit user consent.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the ECU acknowledged the clear.</returns>
    public async Task<bool> ClearCodesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
        string raw = await _transport.SendCommandAsync("04", cancellationToken).ConfigureAwait(false);
        ObdResponse response = ObdResponse.Parse(raw);

        // A positive response to Mode 04 echoes 0x44; some adapters just say OK.
        return response.Status != ObdResponseStatus.Error
            && (response.Data.Contains((byte)0x44)
                || raw.Contains("OK", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<DiagnosticTroubleCode>> ReadCodesAsync(
        string command, byte responseByte, CancellationToken cancellationToken)
    {
        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
        string raw = await _transport.SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        ObdResponse response = ObdResponse.Parse(raw);
        if (response.Status == ObdResponseStatus.NoData || response.Data.Length == 0)
        {
            return [];
        }

        return DtcParser.Parse(response.Data, responseByte);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _transport.DisposeAsync();
}
