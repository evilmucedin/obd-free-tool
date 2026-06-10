namespace ObdFree.Core.Uds;

/// <summary>
/// Parses UDS ReadDTCInformation (service 0x19) payloads into trouble codes.
/// </summary>
public static class UdsDtcParser
{
    /// <summary>UDS request service id for ReadDTCInformation.</summary>
    public const byte ReadDtcService = 0x19;

    /// <summary>Positive response SID for ReadDTCInformation (0x19 + 0x40).</summary>
    public const byte ReadDtcResponse = 0x59;

    /// <summary>Subfunction: reportDTCByStatusMask.</summary>
    public const byte ReportByStatusMask = 0x02;

    /// <summary>Subfunction: reportNumberOfDTCByStatusMask.</summary>
    public const byte ReportNumberByStatusMask = 0x01;

    /// <summary>UDS request service id for ClearDiagnosticInformation.</summary>
    public const byte ClearService = 0x14;

    /// <summary>Positive response SID for ClearDiagnosticInformation (0x14 + 0x40).</summary>
    public const byte ClearResponse = 0x54;

    /// <summary>
    /// Parses a reportDTCByStatusMask (0x19 0x02) response into trouble codes.
    /// </summary>
    /// <param name="response">The parsed UDS response.</param>
    /// <returns>The decoded trouble codes (empty if none).</returns>
    public static IReadOnlyList<UdsDtc> ParseDtcs(UdsResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!response.TryGetPositive(ReadDtcResponse, out byte[] payload) || payload.Length < 1)
        {
            return [];
        }

        // payload[0] = echoed subfunction; payload[1] = statusAvailabilityMask;
        // then groups of 4 bytes: DTC high/mid/low + statusOfDTC.
        int offset = payload.Length >= 1 && payload[0] == ReportByStatusMask ? 2 : 1;
        if (offset > payload.Length)
        {
            return [];
        }

        var codes = new List<UdsDtc>();
        for (int i = offset; i + 3 < payload.Length; i += 4)
        {
            byte a = payload[i];
            byte b = payload[i + 1];
            byte c = payload[i + 2];
            byte status = payload[i + 3];

            if (a == 0 && b == 0 && c == 0)
            {
                continue;
            }

            codes.Add(UdsDtc.Decode(a, b, c, status));
        }

        return codes;
    }

    /// <summary>
    /// Parses a reportNumberOfDTCByStatusMask (0x19 0x01) response into a count.
    /// </summary>
    /// <param name="response">The parsed UDS response.</param>
    /// <param name="count">The number of matching DTCs, if available.</param>
    /// <returns><see langword="true"/> if a count was parsed.</returns>
    public static bool TryParseCount(UdsResponse response, out int count)
    {
        ArgumentNullException.ThrowIfNull(response);
        count = 0;

        if (!response.TryGetPositive(ReadDtcResponse, out byte[] payload) || payload.Length < 5)
        {
            return false;
        }

        // [0]=subfn(0x01) [1]=statusAvailabilityMask [2]=DTCFormatId [3..4]=count.
        count = (payload[3] << 8) | payload[4];
        return true;
    }
}
