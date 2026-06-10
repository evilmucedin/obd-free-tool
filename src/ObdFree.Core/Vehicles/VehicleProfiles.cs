using System.Collections.ObjectModel;

namespace ObdFree.Core.Vehicles;

/// <summary>
/// Built-in vehicle profiles. Toyota and Lexus are first-class because they
/// share the same diagnostic platform and are the initial test targets.
/// </summary>
public static class VehicleProfiles
{
    /// <summary>The fallback profile: let the adapter auto-detect the protocol.</summary>
    public static VehicleProfile Generic { get; } = new(
        "generic",
        "Generic (auto-detect)",
        ObdProtocol.Auto,
        "Works with any OBD-II vehicle; the adapter auto-detects the protocol.");

    /// <summary>Toyota profile (shared with Lexus).</summary>
    public static VehicleProfile Toyota { get; } = new(
        "toyota",
        "Toyota / Lexus",
        ObdProtocol.Can11Bit500k,
        "Most Toyota/Lexus from ~2008+ use ISO 15765-4 CAN (11-bit, 500k). "
        + "For older models, try '--protocol auto' or '--protocol iso9141'.");

    /// <summary>Lexus profile (Toyota's luxury division; identical diagnostics).</summary>
    public static VehicleProfile Lexus { get; } = new(
        "lexus",
        "Lexus / Toyota",
        ObdProtocol.Can11Bit500k,
        "Most Lexus/Toyota from ~2008+ use ISO 15765-4 CAN (11-bit, 500k). "
        + "For older models, try '--protocol auto' or '--protocol iso9141'.");

    private static readonly ReadOnlyDictionary<string, VehicleProfile> ByKeyMap =
        new(new Dictionary<string, VehicleProfile>(StringComparer.OrdinalIgnoreCase)
        {
            [Generic.Key] = Generic,
            [Toyota.Key] = Toyota,
            [Lexus.Key] = Lexus,
        });

    /// <summary>Gets all known profiles.</summary>
    public static IReadOnlyCollection<VehicleProfile> All => ByKeyMap.Values;

    /// <summary>Looks up a profile by key (case-insensitive).</summary>
    /// <param name="key">The profile key, e.g. <c>toyota</c>.</param>
    /// <param name="profile">The matching profile, if found.</param>
    /// <returns><see langword="true"/> if a profile was found.</returns>
    public static bool TryGet(string key, out VehicleProfile? profile)
    {
        ArgumentNullException.ThrowIfNull(key);
        return ByKeyMap.TryGetValue(key, out profile);
    }
}
