namespace ObdFree.Core.Pids;

/// <summary>
/// Describes a single OBD-II parameter: how to request it and how to decode the
/// response bytes into a <see cref="PidValue"/>.
/// </summary>
/// <param name="Key">Short stable identifier used on the CLI, e.g. <c>rpm</c>.</param>
/// <param name="Name">Human-readable name, e.g. <c>Engine RPM</c>.</param>
/// <param name="Mode">OBD mode (almost always <c>0x01</c> for live data).</param>
/// <param name="Pid">The parameter id within the mode.</param>
/// <param name="DataBytes">Number of data bytes expected in the response.</param>
/// <param name="Decode">Decoder mapping the data bytes to a typed value.</param>
public sealed record PidDefinition(
    string Key,
    string Name,
    byte Mode,
    byte Pid,
    int DataBytes,
    Func<byte[], PidValue> Decode)
{
    /// <summary>Gets the hex command sent to the adapter, e.g. <c>010C</c>.</summary>
    public string Command => $"{Mode:X2}{Pid:X2}";
}
