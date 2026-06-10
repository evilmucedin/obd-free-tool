using System.Globalization;

namespace ObdFree.Core.Diagnostics;

/// <summary>
/// A decoded Diagnostic Trouble Code (DTC), e.g. <c>P0133</c>.
/// </summary>
/// <param name="Code">The human-readable code such as <c>P0133</c>.</param>
public readonly record struct DiagnosticTroubleCode(string Code)
{
    private static readonly char[] SystemLetters = ['P', 'C', 'B', 'U'];

    /// <summary>
    /// Decodes a 2-byte DTC (as defined by SAE J2012 / ISO 15031-6) into its
    /// canonical string form such as <c>P0133</c>.
    /// </summary>
    /// <param name="a">The first (high) byte.</param>
    /// <param name="b">The second (low) byte.</param>
    /// <returns>The decoded <see cref="DiagnosticTroubleCode"/>.</returns>
    public static DiagnosticTroubleCode Decode(byte a, byte b)
    {
        // Top two bits of the first byte select the system letter.
        char letter = SystemLetters[(a & 0b1100_0000) >> 6];

        // Next two bits are the first digit (0-3); the remaining nibble and the
        // second byte make up the remaining three hex digits.
        int firstDigit = (a & 0b0011_0000) >> 4;
        int secondDigit = a & 0b0000_1111;
        int thirdDigit = (b & 0b1111_0000) >> 4;
        int fourthDigit = b & 0b0000_1111;

        string code = string.Create(
            CultureInfo.InvariantCulture,
            $"{letter}{firstDigit:X}{secondDigit:X}{thirdDigit:X}{fourthDigit:X}");

        return new DiagnosticTroubleCode(code);
    }

    /// <inheritdoc />
    public override string ToString() => Code;
}
