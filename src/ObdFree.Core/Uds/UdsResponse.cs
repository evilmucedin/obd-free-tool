using System.Text.RegularExpressions;
using ObdFree.Core.Protocol;

namespace ObdFree.Core.Uds;

/// <summary>
/// Cleans and concatenates an ELM327 UDS-over-CAN response into raw bytes.
/// Handles ISO-TP multi-frame output where the adapter prints a frame-index
/// prefix per line (e.g. <c>0:</c>, <c>1:</c>) and an initial length line.
/// </summary>
public sealed partial class UdsResponse
{
    private UdsResponse(string raw, byte[] data, UdsResponseStatus status)
    {
        Raw = raw;
        Data = data;
        Status = status;
    }

    /// <summary>Gets the original adapter text.</summary>
    public string Raw { get; }

    /// <summary>Gets the concatenated payload bytes (frame prefixes stripped).</summary>
    public byte[] Data { get; }

    /// <summary>Gets the high-level status.</summary>
    public UdsResponseStatus Status { get; }

    [GeneratedRegex(@"^[0-9A-Fa-f]{1,2}:\s*")]
    private static partial Regex FramePrefix();

    /// <summary>Parses raw adapter text into a <see cref="UdsResponse"/>.</summary>
    /// <param name="raw">The raw text returned by the adapter.</param>
    /// <returns>The parsed response.</returns>
    public static UdsResponse Parse(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var status = UdsResponseStatus.Ok;
        var bytes = new List<byte>();

        foreach (string rawLine in raw.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string line = rawLine.Replace(">", string.Empty, StringComparison.Ordinal).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string upper = line.ToUpperInvariant();
            if (upper is "NO DATA")
            {
                status = UdsResponseStatus.NoData;
                continue;
            }

            if (upper is "OK")
            {
                continue;
            }

            if (upper is "?" or "ERROR"
                || upper.Contains("UNABLE TO CONNECT", StringComparison.Ordinal)
                || upper.Contains("CAN ERROR", StringComparison.Ordinal)
                || upper.Contains("BUFFER FULL", StringComparison.Ordinal))
            {
                status = UdsResponseStatus.Error;
                continue;
            }

            // Strip an ISO-TP frame-index prefix like "0:" / "1:" if present.
            line = FramePrefix().Replace(line, string.Empty);
            string compact = line.Replace(" ", string.Empty, StringComparison.Ordinal);

            // Keep only clean, even-length hex (drops stray length-only lines).
            if (compact.Length == 0 || (compact.Length & 1) != 0 || !compact.All(Uri.IsHexDigit))
            {
                continue;
            }

            bytes.AddRange(HexBytes.Parse(compact));
        }

        return new UdsResponse(raw, [.. bytes], status);
    }

    /// <summary>
    /// Finds a positive response for <paramref name="serviceId"/> (which is
    /// <c>request + 0x40</c>) and returns the bytes after it. Also detects UDS
    /// negative responses (<c>0x7F</c>).
    /// </summary>
    /// <param name="serviceId">The expected positive response SID (e.g. 0x59).</param>
    /// <param name="payload">The bytes following the SID, if positive.</param>
    /// <returns><see langword="true"/> if a positive response was found.</returns>
    public bool TryGetPositive(byte serviceId, out byte[] payload)
    {
        for (int i = 0; i < Data.Length; i++)
        {
            if (Data[i] == serviceId)
            {
                payload = Data[(i + 1)..];
                return true;
            }
        }

        payload = [];
        return false;
    }
}

/// <summary>High-level classification of a UDS response.</summary>
public enum UdsResponseStatus
{
    /// <summary>The response was understood.</summary>
    Ok,

    /// <summary>The ECU returned <c>NO DATA</c> (often: module not present at this address).</summary>
    NoData,

    /// <summary>The adapter or bus reported an error.</summary>
    Error,
}
