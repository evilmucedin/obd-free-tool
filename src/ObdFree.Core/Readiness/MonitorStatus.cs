namespace ObdFree.Core.Readiness;

/// <summary>Readiness of a single OBD-II emissions monitor.</summary>
/// <param name="Name">The monitor name, e.g. <c>Catalyst</c>.</param>
/// <param name="Supported">Whether the vehicle supports this monitor.</param>
/// <param name="Complete">Whether the monitor has run to completion (is "ready").</param>
public readonly record struct MonitorReadiness(string Name, bool Supported, bool Complete)
{
    /// <summary>Gets a short status word: <c>ready</c>, <c>NOT READY</c>, or <c>n/a</c>.</summary>
    public string StatusText => !Supported ? "n/a" : Complete ? "ready" : "NOT READY";
}

/// <summary>
/// Emissions readiness ("I/M readiness") decoded from Mode 01 PID 01. This is the
/// information a US smog / emissions inspection cares about: the MIL state, the
/// number of stored DTCs, and which monitors have completed.
/// </summary>
/// <param name="MilOn">Whether the MIL ("check engine" light) is commanded on.</param>
/// <param name="DtcCount">Number of confirmed emissions-related DTCs.</param>
/// <param name="IsCompressionIgnition">True for diesel; false for spark/gasoline.</param>
/// <param name="Monitors">The emissions monitors and their readiness.</param>
public sealed record MonitorStatus(
    bool MilOn,
    int DtcCount,
    bool IsCompressionIgnition,
    IReadOnlyList<MonitorReadiness> Monitors)
{
    /// <summary>Gets the supported monitors that have not yet completed.</summary>
    public IReadOnlyList<MonitorReadiness> NotReady =>
        [.. Monitors.Where(m => m.Supported && !m.Complete)];

    /// <summary>Gets the number of supported-but-incomplete monitors.</summary>
    public int NotReadyCount => NotReady.Count;

    /// <summary>
    /// Rough guidance on whether the vehicle would pass a US OBD emissions check:
    /// the MIL must be off and most states allow at most one incomplete monitor
    /// (two for pre-2000 vehicles). This is guidance only — exact rules vary by
    /// state, so always confirm locally.
    /// </summary>
    public bool LikelyReadyForInspection => !MilOn && NotReadyCount <= 1;
}
