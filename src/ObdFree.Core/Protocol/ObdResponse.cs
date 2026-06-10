namespace ObdFree.Core.Protocol;

/// <summary>
/// A parsed ELM327 response. Cleans away echoes, prompts, status chatter
/// (<c>SEARCHING...</c>) and whitespace, and exposes the decoded data bytes.
/// </summary>
public sealed class ObdResponse
{
    private static readonly string[] NoiseTokens =
    [
        "SEARCHING", "BUS INIT", "BUSINIT", "STOPPED",
    ];

    private ObdResponse(string raw, IReadOnlyList<string> dataLines, byte[] data, ObdResponseStatus status)
    {
        Raw = raw;
        DataLines = dataLines;
        Data = data;
        Status = status;
    }

    /// <summary>Gets the original, untouched adapter text.</summary>
    public string Raw { get; }

    /// <summary>Gets the cleaned hex data lines (noise and prompts removed).</summary>
    public IReadOnlyList<string> DataLines { get; }

    /// <summary>Gets the concatenated decoded data bytes across all data lines.</summary>
    public byte[] Data { get; }

    /// <summary>Gets the high-level status of the response.</summary>
    public ObdResponseStatus Status { get; }

    /// <summary>Gets a value indicating whether the response carries usable data.</summary>
    public bool IsSuccess => Status == ObdResponseStatus.Ok && Data.Length > 0;

    /// <summary>
    /// Parses raw adapter text into an <see cref="ObdResponse"/>.
    /// </summary>
    /// <param name="raw">The raw text returned by the adapter.</param>
    /// <returns>The parsed response.</returns>
    public static ObdResponse Parse(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var dataLines = new List<string>();
        var status = ObdResponseStatus.Ok;
        var combined = new List<byte>();

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
                status = ObdResponseStatus.NoData;
                continue;
            }

            if (upper is "OK")
            {
                continue;
            }

            if (upper is "?" or "ERROR" || upper.Contains("UNABLE TO CONNECT", StringComparison.Ordinal)
                || upper.Contains("CAN ERROR", StringComparison.Ordinal))
            {
                status = ObdResponseStatus.Error;
                continue;
            }

            if (NoiseTokens.Any(t => upper.Contains(t, StringComparison.Ordinal)))
            {
                continue;
            }

            // A real data line is hex (after stripping spaces).
            string compact = line.Replace(" ", string.Empty, StringComparison.Ordinal);
            if (compact.Length == 0 || !compact.All(Uri.IsHexDigit) || (compact.Length & 1) != 0)
            {
                continue;
            }

            dataLines.Add(compact);
            combined.AddRange(HexBytes.Parse(compact));
        }

        return new ObdResponse(raw, dataLines, [.. combined], status);
    }
}

/// <summary>High-level classification of an ELM327 response.</summary>
public enum ObdResponseStatus
{
    /// <summary>The response was understood (may or may not carry data).</summary>
    Ok,

    /// <summary>The ECU reported <c>NO DATA</c> for the request.</summary>
    NoData,

    /// <summary>The adapter or bus reported an error.</summary>
    Error,
}
