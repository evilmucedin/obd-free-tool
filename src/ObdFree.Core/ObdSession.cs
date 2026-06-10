using ObdFree.Core.Adapters;
using ObdFree.Core.Diagnostics;
using ObdFree.Core.Pids;
using ObdFree.Core.Protocol;
using ObdFree.Core.Readiness;
using ObdFree.Core.Transport;
using ObdFree.Core.Uds;
using ObdFree.Core.VehicleInfo;
using ObdFree.Core.Vehicles;

namespace ObdFree.Core;

/// <summary>A single live parameter reading.</summary>
/// <param name="Definition">The parameter that was read.</param>
/// <param name="Value">The decoded value.</param>
public readonly record struct LiveReading(PidDefinition Definition, PidValue Value);

/// <summary>Status of a UDS module (e.g. SRS/airbag): its codes and a derived warning flag.</summary>
/// <param name="Module">The module that was queried.</param>
/// <param name="Codes">The trouble codes reported by the module.</param>
public sealed record ModuleStatus(EcuModule Module, IReadOnlyList<UdsDtc> Codes)
{
    /// <summary>Gets a value indicating whether the module reported any trouble codes.</summary>
    public bool HasFaults => Codes.Count > 0;

    /// <summary>Gets a short human-readable summary, e.g. <c>OK</c> or <c>2 code(s)</c>.</summary>
    public string Summary => HasFaults ? $"{Codes.Count} code(s)" : "OK (no codes)";
}

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
public sealed class ObdSession(
    IObdTransport transport,
    VehicleProfile? profile = null,
    AdapterProfile? adapter = null) : IAsyncDisposable
{
    private readonly IObdTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    /// <summary>Gets the vehicle profile guiding this session (defaults to generic).</summary>
    public VehicleProfile Profile { get; } = profile ?? VehicleProfiles.Generic;

    /// <summary>Gets the adapter profile (reset/timing tuning; defaults to standard).</summary>
    public AdapterProfile Adapter { get; } = adapter ?? AdapterProfiles.Standard;

    /// <summary>Gets the adapter identity from the last <see cref="ConnectAsync"/>, if any.</summary>
    public string? AdapterIdentity { get; private set; }

    /// <summary>
    /// Gets whether the connected device looks like an ELM327-compatible adapter.
    /// <see langword="false"/> typically means a proprietary dongle (e.g. Launch
    /// DBSCAR) that this tool cannot drive.
    /// </summary>
    public bool AdapterLooksElmCompatible => AdapterCompatibility.IsLikelyElm(AdapterIdentity);

    private async Task EnsureOpenAsync(CancellationToken cancellationToken)
    {
        if (!_transport.IsOpen)
        {
            await _transport.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string> InitStepAsync(string command, CancellationToken cancellationToken)
    {
        string response = await _transport.SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        if (Adapter.InterCommandDelayMs > 0)
        {
            await Task.Delay(Adapter.InterCommandDelayMs, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    /// <summary>
    /// Opens the transport and runs the ELM327 initialization sequence (reset
    /// per the adapter profile, echo/linefeed/spaces/headers off, then sets the
    /// vehicle profile's preferred protocol). Records the adapter identity.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The adapter identity reported by <c>ATI</c>.</returns>
    public async Task<string> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);

        // Reset (ATZ or ATWS), with an optional settle delay for finicky adapters.
        await _transport.SendCommandAsync(Adapter.ResetCommand, cancellationToken).ConfigureAwait(false);
        if (Adapter.ResetDelayMs > 0)
        {
            await Task.Delay(Adapter.ResetDelayMs, cancellationToken).ConfigureAwait(false);
        }

        await InitStepAsync("ATE0", cancellationToken).ConfigureAwait(false);  // echo off
        await InitStepAsync("ATL0", cancellationToken).ConfigureAwait(false);  // linefeeds off
        await InitStepAsync("ATS0", cancellationToken).ConfigureAwait(false);  // spaces off
        await InitStepAsync("ATH0", cancellationToken).ConfigureAwait(false);  // headers off

        // Set the protocol for this vehicle (ATSP0 = auto, ATSP6 = CAN 11/500, …).
        await InitStepAsync(Profile.PreferredProtocol.ToSetProtocolCommand(), cancellationToken).ConfigureAwait(false);

        string identity = (await _transport.SendCommandAsync("ATI", cancellationToken).ConfigureAwait(false))
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal)
            .Trim();

        AdapterIdentity = string.IsNullOrWhiteSpace(identity) ? null : identity;
        return AdapterIdentity ?? "Unknown device (no ATI response)";
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
    /// Reads permanent trouble codes (Mode 0A). Permanent codes cannot be cleared
    /// by a scan tool or by disconnecting the battery — they clear only after the
    /// vehicle confirms the fault is fixed. US emissions inspections check these.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The permanent trouble codes.</returns>
    public Task<IReadOnlyList<DiagnosticTroubleCode>> ReadPermanentCodesAsync(CancellationToken cancellationToken = default)
        => ReadCodesAsync("0A", DtcParser.PermanentResponseByte, cancellationToken);

    /// <summary>
    /// Reads emissions readiness ("I/M readiness") and MIL state (Mode 01 PID 01)
    /// — the core information for a US smog / emissions inspection.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The readiness status, or <see langword="null"/> if unavailable.</returns>
    public async Task<MonitorStatus?> ReadReadinessAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
        string raw = await _transport.SendCommandAsync("0101", cancellationToken).ConfigureAwait(false);
        ObdResponse response = ObdResponse.Parse(raw);
        if (!response.IsSuccess
            || !ObdPayload.TryGetParameter(response.Data, 0x01, 0x01, out byte[] payload)
            || payload.Length < 4)
        {
            return null;
        }

        return MonitorStatusDecoder.Decode(payload[0], payload[1], payload[2], payload[3]);
    }

    /// <summary>Reads the Vehicle Identification Number (VIN) — Mode 09 PID 02.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The VIN, or <see langword="null"/> if unavailable.</returns>
    public async Task<string?> ReadVinAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
        string raw = await _transport.SendCommandAsync("0902", cancellationToken).ConfigureAwait(false);

        // Mode 09 VIN is multi-frame; UdsResponse cleans ISO-TP frame formatting.
        return VinDecoder.Decode(UdsResponse.Parse(raw).Data);
    }

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

    /// <summary>
    /// Reads trouble codes from a UDS module (e.g. the SRS/airbag ECU) over CAN.
    /// Not part of generic OBD-II — addresses are make-specific. Defaults to the
    /// Toyota/Lexus SRS module when <paramref name="module"/> is null.
    /// </summary>
    /// <param name="module">The module to query, or null for Toyota/Lexus SRS.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The module status (codes + derived warning flag).</returns>
    public async Task<ModuleStatus> ReadModuleStatusAsync(EcuModule? module = null, CancellationToken cancellationToken = default)
    {
        EcuModule target = module ?? ToyotaModules.Srs;
        await ConnectAsync(cancellationToken).ConfigureAwait(false);

        var uds = new UdsClient(_transport);
        await uds.ConfigureAsync(target, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<UdsDtc> codes = await uds.ReadDtcsAsync(cancellationToken).ConfigureAwait(false);
        return new ModuleStatus(target, codes);
    }

    /// <summary>
    /// Clears trouble codes from a UDS module (e.g. the SRS/airbag ECU).
    /// <b>Write operation</b> — only after the underlying fault is repaired.
    /// Defaults to the Toyota/Lexus SRS module when <paramref name="module"/> is null.
    /// </summary>
    /// <param name="module">The module to clear, or null for Toyota/Lexus SRS.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the module acknowledged the clear.</returns>
    public async Task<bool> ClearModuleCodesAsync(EcuModule? module = null, CancellationToken cancellationToken = default)
    {
        EcuModule target = module ?? ToyotaModules.Srs;
        await ConnectAsync(cancellationToken).ConfigureAwait(false);

        var uds = new UdsClient(_transport);
        await uds.ConfigureAsync(target, cancellationToken).ConfigureAwait(false);
        return await uds.ClearDtcsAsync(cancellationToken).ConfigureAwait(false);
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
