using ObdFree.Core.Transport;

namespace ObdFree.Core.Uds;

/// <summary>
/// A minimal UDS-over-CAN (ISO 14229 / ISO 15765-4) client built on top of an
/// ELM327 transport. Targets a specific <see cref="EcuModule"/> (e.g. SRS) by
/// setting the CAN request/response headers, then reads or clears its DTCs.
/// </summary>
public sealed class UdsClient(IObdTransport transport)
{
    private readonly IObdTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    /// <summary>
    /// Points the adapter at <paramref name="module"/>: forces CAN 11-bit/500k,
    /// sets the transmit header and receive-address filter, and configures ISO-TP
    /// flow control so multi-frame responses are assembled.
    /// </summary>
    /// <param name="module">The target ECU module.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task ConfigureAsync(EcuModule module, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(module);

        await _transport.SendCommandAsync("ATSP6", cancellationToken).ConfigureAwait(false);                 // CAN 11/500
        await _transport.SendCommandAsync("ATSH" + module.RequestHeader, cancellationToken).ConfigureAwait(false);   // tx header
        await _transport.SendCommandAsync("ATCRA" + module.ResponseHeader, cancellationToken).ConfigureAwait(false);  // rx filter
        await _transport.SendCommandAsync("ATFCSH" + module.RequestHeader, cancellationToken).ConfigureAwait(false);  // flow-control header
        await _transport.SendCommandAsync("ATFCSD300000", cancellationToken).ConfigureAwait(false);          // flow-control data
        await _transport.SendCommandAsync("ATFCSM1", cancellationToken).ConfigureAwait(false);               // flow-control mode 1
    }

    /// <summary>Reads stored DTCs from the configured module (reportDTCByStatusMask).</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The decoded trouble codes.</returns>
    public async Task<IReadOnlyList<UdsDtc>> ReadDtcsAsync(CancellationToken cancellationToken = default)
    {
        string raw = await _transport.SendCommandAsync("1902FF", cancellationToken).ConfigureAwait(false);
        return UdsDtcParser.ParseDtcs(UdsResponse.Parse(raw));
    }

    /// <summary>Reads the number of DTCs reported by the module (reportNumberOfDTC).</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The count, or <see langword="null"/> if unavailable.</returns>
    public async Task<int?> ReadDtcCountAsync(CancellationToken cancellationToken = default)
    {
        string raw = await _transport.SendCommandAsync("1901FF", cancellationToken).ConfigureAwait(false);
        return UdsDtcParser.TryParseCount(UdsResponse.Parse(raw), out int count) ? count : null;
    }

    /// <summary>
    /// Clears all DTCs from the configured module (ClearDiagnosticInformation,
    /// group 0xFFFFFF). <b>Write operation</b> — only after the fault is repaired.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the ECU acknowledged the clear.</returns>
    public async Task<bool> ClearDtcsAsync(CancellationToken cancellationToken = default)
    {
        string raw = await _transport.SendCommandAsync("14FFFFFF", cancellationToken).ConfigureAwait(false);
        UdsResponse response = UdsResponse.Parse(raw);
        return response.Status != UdsResponseStatus.Error
            && response.TryGetPositive(UdsDtcParser.ClearResponse, out _);
    }
}
