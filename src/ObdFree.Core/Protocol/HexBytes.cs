using System.Globalization;

namespace ObdFree.Core.Protocol;

/// <summary>
/// Helpers for converting between ELM327 ASCII-hex text and raw bytes.
/// </summary>
public static class HexBytes
{
    /// <summary>
    /// Parses a string of hexadecimal digits (optionally separated by spaces)
    /// into bytes. Any non-hex characters are ignored, which makes it tolerant
    /// of adapter formatting differences.
    /// </summary>
    /// <param name="text">The hex text, e.g. <c>"41 0C 0C E4"</c> or <c>"410C0CE4"</c>.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="FormatException">An odd number of hex digits was found.</exception>
    public static byte[] Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Span<char> digits = text.Length <= 256 ? stackalloc char[text.Length] : new char[text.Length];
        int count = 0;
        foreach (char c in text)
        {
            if (Uri.IsHexDigit(c))
            {
                digits[count++] = c;
            }
        }

        if ((count & 1) != 0)
        {
            throw new FormatException($"Hex text has an odd number of digits: '{text}'.");
        }

        byte[] result = new byte[count / 2];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = byte.Parse(digits.Slice(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return result;
    }

    /// <summary>Formats bytes as uppercase hex with no separators, e.g. <c>"010C"</c>.</summary>
    /// <param name="bytes">The bytes to format.</param>
    /// <returns>The uppercase hex string.</returns>
    public static string ToHex(ReadOnlySpan<byte> bytes) => Convert.ToHexString(bytes);
}
