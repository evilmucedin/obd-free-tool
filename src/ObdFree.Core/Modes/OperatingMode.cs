namespace ObdFree.Core.Modes;

/// <summary>
/// How much the app is allowed to do. <see cref="Safe"/> is the default and
/// sticks to read-only, standard OBD-II. <see cref="Professional"/> unlocks
/// write operations and manufacturer-specific / experimental features, some of
/// which can be risky.
/// </summary>
public enum OperatingMode
{
    /// <summary>Read-only, standard OBD-II. The safe default.</summary>
    Safe,

    /// <summary>Unlocks writes and advanced/experimental features (use with care).</summary>
    Professional,
}

/// <summary>
/// A capability the app can perform. Each maps to a minimum
/// <see cref="OperatingMode"/> via <see cref="ModePolicy"/>.
/// </summary>
public enum AppFeature
{
    /// <summary>Read adapter status and live data (safe).</summary>
    ReadStatus,

    /// <summary>Read emissions readiness / MIL (safe).</summary>
    ReadReadiness,

    /// <summary>Read the VIN (safe).</summary>
    ReadVin,

    /// <summary>Read diagnostic trouble codes (safe).</summary>
    ReadDtc,

    /// <summary>Clear diagnostic trouble codes — write, resets emissions readiness (professional).</summary>
    ClearDtc,

    /// <summary>Read non-OBD modules such as SRS/airbag over UDS — advanced/experimental (professional).</summary>
    SrsRead,

    /// <summary>Clear SRS/airbag codes — dangerous write to a safety system (professional).</summary>
    SrsClear,
}
