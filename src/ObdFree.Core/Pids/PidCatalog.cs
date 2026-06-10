using System.Collections.ObjectModel;

namespace ObdFree.Core.Pids;

/// <summary>
/// Catalog of common SAE J1979 Mode 01 parameters supported by the tool,
/// keyed by a short stable identifier.
/// </summary>
public static class PidCatalog
{
    private static readonly ReadOnlyDictionary<string, PidDefinition> ByKeyMap = Build();

    /// <summary>Gets all known parameter definitions.</summary>
    public static IReadOnlyCollection<PidDefinition> All => ByKeyMap.Values;

    /// <summary>Looks up a parameter definition by its key (case-insensitive).</summary>
    /// <param name="key">The parameter key, e.g. <c>rpm</c>.</param>
    /// <param name="definition">The matching definition, if found.</param>
    /// <returns><see langword="true"/> if a definition was found.</returns>
    public static bool TryGet(string key, out PidDefinition? definition)
    {
        ArgumentNullException.ThrowIfNull(key);
        return ByKeyMap.TryGetValue(key.ToLowerInvariant(), out definition);
    }

    private static ReadOnlyDictionary<string, PidDefinition> Build()
    {
        PidDefinition[] definitions =
        [
            new("load", "Engine load", 0x01, 0x04, 1, d => PidDecoders.EngineLoad(d[0])),
            new("coolant_temp", "Coolant temperature", 0x01, 0x05, 1, d => PidDecoders.CoolantTemperature(d[0])),
            new("rpm", "Engine RPM", 0x01, 0x0C, 2, d => PidDecoders.EngineRpm(d[0], d[1])),
            new("speed", "Vehicle speed", 0x01, 0x0D, 1, d => PidDecoders.VehicleSpeed(d[0])),
            new("intake_temp", "Intake air temperature", 0x01, 0x0F, 1, d => PidDecoders.CoolantTemperature(d[0])),
            new("throttle", "Throttle position", 0x01, 0x11, 1, d => PidDecoders.ThrottlePosition(d[0])),
        ];

        var map = new Dictionary<string, PidDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (PidDefinition def in definitions)
        {
            map[def.Key] = def;
        }

        return new ReadOnlyDictionary<string, PidDefinition>(map);
    }
}
