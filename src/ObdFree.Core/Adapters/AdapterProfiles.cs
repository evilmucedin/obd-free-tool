using System.Collections.ObjectModel;

namespace ObdFree.Core.Adapters;

/// <summary>Built-in adapter profiles.</summary>
public static class AdapterProfiles
{
    /// <summary>Standard ELM327 / STN behavior: full reset, no artificial delays.</summary>
    public static AdapterProfile Standard { get; } = new(
        "standard",
        "Standard ELM327 / STN",
        "ATZ",
        ResetDelayMs: 0,
        InterCommandDelayMs: 0,
        "Genuine ELM327, STN (OBDLink), and well-behaved clones.");

    /// <summary>
    /// Tolerant profile for cheap clones and ELM327-compatible Launch Wi-Fi/BT
    /// dongles: warm-start reset and conservative timing so the bridge keeps up.
    /// </summary>
    public static AdapterProfile Launch { get; } = new(
        "launch",
        "Launch / clone (ELM327-compatible, tolerant timing)",
        "ATWS",
        ResetDelayMs: 1200,
        InterCommandDelayMs: 80,
        "For ELM327-compatible Launch Wi-Fi/BT units and finicky clones. "
        + "Proprietary DBSCAR dongles (Thinkdiag/Easydiag/Golo) are NOT ELM327 "
        + "and cannot be used here — see docs.");

    private static readonly ReadOnlyDictionary<string, AdapterProfile> ByKeyMap =
        new(new Dictionary<string, AdapterProfile>(StringComparer.OrdinalIgnoreCase)
        {
            [Standard.Key] = Standard,
            [Launch.Key] = Launch,
        });

    /// <summary>Gets all known adapter profiles.</summary>
    public static IReadOnlyCollection<AdapterProfile> All => ByKeyMap.Values;

    /// <summary>Looks up an adapter profile by key (case-insensitive).</summary>
    /// <param name="key">The profile key, e.g. <c>launch</c>.</param>
    /// <param name="profile">The matching profile, if found.</param>
    /// <returns><see langword="true"/> if a profile was found.</returns>
    public static bool TryGet(string key, out AdapterProfile? profile)
    {
        ArgumentNullException.ThrowIfNull(key);
        return ByKeyMap.TryGetValue(key, out profile);
    }
}
