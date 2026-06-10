namespace ObdFree.Core.Adapters;

/// <summary>
/// Heuristics for deciding whether a connected device is actually an
/// ELM327-compatible adapter, based on its identity string (<c>ATI</c>).
/// </summary>
public static class AdapterCompatibility
{
    private static readonly string[] ElmMarkers =
    [
        "ELM327", "ELM32", "STN", "OBDLINK", "OBDII", "OBD-II", "ICAR", "VLINK", "SCANTOOL",
    ];

    /// <summary>
    /// Returns whether an adapter identity looks like an ELM327-compatible
    /// device. Proprietary dongles (e.g. Launch DBSCAR / Thinkdiag) return
    /// garbage or nothing here.
    /// </summary>
    /// <param name="identity">The identity string reported by <c>ATI</c>.</param>
    /// <returns><see langword="true"/> if it looks ELM327-compatible.</returns>
    public static bool IsLikelyElm(string? identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            return false;
        }

        string upper = identity.ToUpperInvariant();
        return ElmMarkers.Any(m => upper.Contains(m, StringComparison.Ordinal));
    }

    /// <summary>
    /// A user-facing hint shown when the connected device does not look like an
    /// ELM327 adapter — most often a proprietary Launch dongle.
    /// </summary>
    public const string ProprietaryAdapterHint =
        "This device does not respond like an ELM327 adapter. Popular Launch "
        + "dongles (Thinkdiag, Easydiag, Golo, X431/DBSCAR) use a proprietary "
        + "protocol and are locked to Launch's apps — they cannot be used here. "
        + "Use an ELM327-compatible adapter (e.g. OBDLink, vLinker, Vgate), or an "
        + "ELM327-compatible Launch Wi-Fi unit with '--adapter launch'.";
}
