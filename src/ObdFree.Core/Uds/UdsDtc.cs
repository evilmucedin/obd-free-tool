using System.Globalization;
using ObdFree.Core.Diagnostics;

namespace ObdFree.Core.Uds;

/// <summary>
/// A trouble code reported by a UDS ECU. The 3-byte ISO 14229 DTC decodes to a
/// familiar 5-character code (e.g. <c>B0100</c>) plus a one-byte failure type
/// (the suffix Toyota/Lexus shows as, e.g., <c>B0100-13</c>), with a status byte.
/// </summary>
/// <param name="Code">The 5-character code, e.g. <c>B0100</c>.</param>
/// <param name="FailureType">The failure-type byte (the suffix after the code).</param>
/// <param name="Status">The UDS status-of-DTC byte (bit 0 = test failed, …).</param>
public readonly record struct UdsDtc(string Code, byte FailureType, byte Status)
{
    /// <summary>Decodes a 3-byte UDS DTC plus its status byte.</summary>
    /// <param name="a">DTC high byte.</param>
    /// <param name="b">DTC middle byte.</param>
    /// <param name="c">DTC low byte (failure type).</param>
    /// <param name="status">The status-of-DTC byte.</param>
    /// <returns>The decoded <see cref="UdsDtc"/>.</returns>
    public static UdsDtc Decode(byte a, byte b, byte c, byte status)
        => new(DiagnosticTroubleCode.Decode(a, b).Code, c, status);

    /// <summary>Gets a value indicating whether the DTC's "test failed" bit is set.</summary>
    public bool IsActive => (Status & 0x01) != 0;

    /// <inheritdoc />
    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"{Code}-{FailureType:X2}");
}
