using System.Text;

namespace ObdFree.Core.VehicleInfo;

/// <summary>
/// Decodes the Vehicle Identification Number (VIN) from a Mode 09 PID 02
/// response. The reply (<c>49 02 ...</c>) is multi-frame; the VIN is 17 ASCII
/// characters carried after a message-count byte.
/// </summary>
public static class VinDecoder
{
    /// <summary>Positive response byte for Mode 09.</summary>
    public const byte ResponseByte = 0x49;

    /// <summary>The Mode 09 PID for VIN.</summary>
    public const byte VinPid = 0x02;

    /// <summary>
    /// Extracts the VIN from already-cleaned response bytes.
    /// </summary>
    /// <param name="data">The concatenated response bytes (frames reassembled).</param>
    /// <returns>The VIN, or <see langword="null"/> if not present.</returns>
    public static string? Decode(ReadOnlySpan<byte> data)
    {
        for (int i = 0; i + 1 < data.Length; i++)
        {
            if (data[i] != ResponseByte || data[i + 1] != VinPid)
            {
                continue;
            }

            // After "49 02" comes a message-count byte (e.g. 0x01, non-printable),
            // then the VIN as printable ASCII. Collect printable chars only.
            var sb = new StringBuilder(17);
            foreach (byte b in data[(i + 2)..])
            {
                if (b is >= 0x20 and < 0x7F)
                {
                    sb.Append((char)b);
                }
            }

            string vin = sb.ToString().Trim();
            return vin.Length >= 11 ? vin : null;
        }

        return null;
    }
}
