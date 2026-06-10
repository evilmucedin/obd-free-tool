namespace ObdFree.Core.Vehicles;

/// <summary>
/// OBD-II signaling protocols, with values matching the ELM327 <c>ATSP</c>
/// (Set Protocol) code so they map directly to the adapter command.
/// </summary>
public enum ObdProtocol
{
    /// <summary>Let the adapter auto-detect the protocol (<c>ATSP0</c>).</summary>
    Auto = 0,

    /// <summary>SAE J1850 PWM (41.6 kbaud) — <c>ATSP1</c>.</summary>
    J1850Pwm = 1,

    /// <summary>SAE J1850 VPW (10.4 kbaud) — <c>ATSP2</c>.</summary>
    J1850Vpw = 2,

    /// <summary>ISO 9141-2 — <c>ATSP3</c>.</summary>
    Iso9141 = 3,

    /// <summary>ISO 14230-4 KWP (5-baud init) — <c>ATSP4</c>.</summary>
    Kwp2000SlowInit = 4,

    /// <summary>ISO 14230-4 KWP (fast init) — <c>ATSP5</c>.</summary>
    Kwp2000FastInit = 5,

    /// <summary>ISO 15765-4 CAN, 11-bit ID, 500 kbaud — <c>ATSP6</c>.</summary>
    Can11Bit500k = 6,

    /// <summary>ISO 15765-4 CAN, 29-bit ID, 500 kbaud — <c>ATSP7</c>.</summary>
    Can29Bit500k = 7,

    /// <summary>ISO 15765-4 CAN, 11-bit ID, 250 kbaud — <c>ATSP8</c>.</summary>
    Can11Bit250k = 8,

    /// <summary>ISO 15765-4 CAN, 29-bit ID, 250 kbaud — <c>ATSP9</c>.</summary>
    Can29Bit250k = 9,
}

/// <summary>Helpers for <see cref="ObdProtocol"/>.</summary>
public static class ObdProtocolExtensions
{
    /// <summary>Builds the ELM327 <c>ATSP</c> command for a protocol (e.g. <c>ATSP6</c>).</summary>
    /// <param name="protocol">The protocol.</param>
    /// <returns>The <c>ATSP</c> command string.</returns>
    public static string ToSetProtocolCommand(this ObdProtocol protocol)
        => $"ATSP{(int)protocol}";

    /// <summary>
    /// Parses a friendly protocol name (e.g. <c>auto</c>, <c>can</c>,
    /// <c>iso9141</c>) or a raw ELM <c>ATSP</c> digit (<c>0</c>–<c>9</c>).
    /// </summary>
    /// <param name="text">The protocol name or digit.</param>
    /// <param name="protocol">The parsed protocol.</param>
    /// <returns><see langword="true"/> if the text was recognized.</returns>
    public static bool TryParse(string text, out ObdProtocol protocol)
    {
        protocol = ObdProtocol.Auto;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        switch (text.Trim().ToLowerInvariant())
        {
            case "auto" or "0":
                protocol = ObdProtocol.Auto;
                return true;
            case "can" or "can11" or "can11_500" or "6":
                protocol = ObdProtocol.Can11Bit500k;
                return true;
            case "can29" or "can29_500" or "7":
                protocol = ObdProtocol.Can29Bit500k;
                return true;
            case "can11_250" or "8":
                protocol = ObdProtocol.Can11Bit250k;
                return true;
            case "can29_250" or "9":
                protocol = ObdProtocol.Can29Bit250k;
                return true;
            case "iso9141" or "iso" or "3":
                protocol = ObdProtocol.Iso9141;
                return true;
            case "kwp" or "kwp_fast" or "5":
                protocol = ObdProtocol.Kwp2000FastInit;
                return true;
            case "kwp_slow" or "4":
                protocol = ObdProtocol.Kwp2000SlowInit;
                return true;
            case "j1850pwm" or "1":
                protocol = ObdProtocol.J1850Pwm;
                return true;
            case "j1850vpw" or "2":
                protocol = ObdProtocol.J1850Vpw;
                return true;
            default:
                return false;
        }
    }
}
