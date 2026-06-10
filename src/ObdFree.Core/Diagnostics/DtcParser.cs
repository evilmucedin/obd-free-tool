namespace ObdFree.Core.Diagnostics;

/// <summary>
/// Parses the data bytes of a Mode 03 (stored) or Mode 07 (pending) response
/// into a list of <see cref="DiagnosticTroubleCode"/>.
/// </summary>
public static class DtcParser
{
    /// <summary>Positive-response byte for Mode 03 (stored DTCs).</summary>
    public const byte StoredResponseByte = 0x43;

    /// <summary>Positive-response byte for Mode 07 (pending DTCs).</summary>
    public const byte PendingResponseByte = 0x47;

    /// <summary>Positive-response byte for Mode 0A (permanent DTCs).</summary>
    public const byte PermanentResponseByte = 0x4A;

    /// <summary>
    /// Parses combined response bytes into trouble codes.
    /// </summary>
    /// <param name="data">
    /// The combined data bytes of the response, beginning with the positive
    /// response byte (e.g. <c>0x43</c>).
    /// </param>
    /// <param name="responseByte">The expected positive-response byte.</param>
    /// <returns>The decoded trouble codes (empty if none are present).</returns>
    public static IReadOnlyList<DiagnosticTroubleCode> Parse(ReadOnlySpan<byte> data, byte responseByte)
    {
        // Locate the positive-response byte; everything before it is preamble.
        int start = data.IndexOf(responseByte);
        if (start < 0)
        {
            return [];
        }

        ReadOnlySpan<byte> body = data[(start + 1)..];

        // On CAN, a count byte follows the response byte, making the remaining
        // length odd. Drop it so DTCs pair up cleanly. (J1979 / ISO 15765-4.)
        if ((body.Length & 1) != 0 && body.Length > 0)
        {
            body = body[1..];
        }

        var codes = new List<DiagnosticTroubleCode>(body.Length / 2);
        for (int i = 0; i + 1 < body.Length; i += 2)
        {
            byte a = body[i];
            byte b = body[i + 1];

            // 0x0000 is padding / "no more codes".
            if (a == 0 && b == 0)
            {
                continue;
            }

            codes.Add(DiagnosticTroubleCode.Decode(a, b));
        }

        return codes;
    }
}
