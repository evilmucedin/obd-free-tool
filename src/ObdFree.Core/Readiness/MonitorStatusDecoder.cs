namespace ObdFree.Core.Readiness;

/// <summary>
/// Decodes the 4 data bytes (A, B, C, D) of Mode 01 PID 01
/// ("Monitor status since DTCs cleared") into a <see cref="MonitorStatus"/>,
/// per SAE J1979.
/// </summary>
public static class MonitorStatusDecoder
{
    // Non-continuous monitor names by bit position (null = reserved / skip).
    private static readonly string?[] SparkMonitors =
    [
        "Catalyst", "Heated Catalyst", "Evaporative System", "Secondary Air System",
        "A/C Refrigerant", "Oxygen Sensor", "Oxygen Sensor Heater", "EGR System",
    ];

    private static readonly string?[] CompressionMonitors =
    [
        "NMHC Catalyst", "NOx/SCR Aftertreatment", null, "Boost Pressure",
        null, "Exhaust Gas Sensor", "PM Filter", "EGR/VVT System",
    ];

    /// <summary>Decodes the four PID-01 data bytes.</summary>
    /// <param name="a">Byte A: MIL flag (bit 7) and DTC count (bits 6-0).</param>
    /// <param name="b">Byte B: continuous monitors + ignition type.</param>
    /// <param name="c">Byte C: which non-continuous monitors are supported.</param>
    /// <param name="d">Byte D: non-continuous monitor completeness (1 = incomplete).</param>
    /// <returns>The decoded readiness status.</returns>
    public static MonitorStatus Decode(byte a, byte b, byte c, byte d)
    {
        bool milOn = (a & 0x80) != 0;
        int dtcCount = a & 0x7F;
        bool compression = (b & 0x08) != 0;

        var monitors = new List<MonitorReadiness>
        {
            // Continuous monitors: support bits 0-2, "incomplete" bits 4-6.
            new("Misfire", (b & 0x01) != 0, (b & 0x10) == 0),
            new("Fuel System", (b & 0x02) != 0, (b & 0x20) == 0),
            new("Comprehensive Components", (b & 0x04) != 0, (b & 0x40) == 0),
        };

        string?[] names = compression ? CompressionMonitors : SparkMonitors;
        for (int bit = 0; bit < 8; bit++)
        {
            if (names[bit] is not { } name)
            {
                continue;
            }

            bool supported = (c & (1 << bit)) != 0;
            bool complete = (d & (1 << bit)) == 0;
            monitors.Add(new MonitorReadiness(name, supported, complete));
        }

        return new MonitorStatus(milOn, dtcCount, compression, monitors);
    }
}
