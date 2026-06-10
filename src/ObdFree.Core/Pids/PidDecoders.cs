namespace ObdFree.Core.Pids;

/// <summary>
/// A decoded parameter value with its unit, e.g. <c>825 rpm</c>.
/// </summary>
/// <param name="Value">The numeric value.</param>
/// <param name="Unit">The unit of measurement.</param>
public readonly record struct PidValue(double Value, string Unit)
{
    /// <inheritdoc />
    public override string ToString() => $"{Value} {Unit}";
}

/// <summary>
/// Pure decoders for standard SAE J1979 Mode 01 PIDs. These are intentionally
/// side-effect free so they are trivial to unit test.
/// </summary>
public static class PidDecoders
{
    /// <summary>Engine speed, PID 0C: <c>((A * 256) + B) / 4</c> rpm.</summary>
    public static PidValue EngineRpm(byte a, byte b)
        => new(((a * 256) + b) / 4.0, "rpm");

    /// <summary>Vehicle speed, PID 0D: <c>A</c> km/h.</summary>
    public static PidValue VehicleSpeed(byte a)
        => new(a, "km/h");

    /// <summary>Engine coolant temperature, PID 05: <c>A - 40</c> °C.</summary>
    public static PidValue CoolantTemperature(byte a)
        => new(a - 40, "\u00B0C");

    /// <summary>Calculated engine load, PID 04: <c>A * 100 / 255</c> %.</summary>
    public static PidValue EngineLoad(byte a)
        => new(a * 100.0 / 255.0, "%");

    /// <summary>Throttle position, PID 11: <c>A * 100 / 255</c> %.</summary>
    public static PidValue ThrottlePosition(byte a)
        => new(a * 100.0 / 255.0, "%");
}
