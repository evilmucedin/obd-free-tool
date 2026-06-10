using System.Collections.ObjectModel;

namespace ObdFree.Core.Adapters;

/// <summary>
/// Catalog of popular off-the-shelf OBD-II dongles (the common Amazon sellers),
/// so the apps can auto-configure the right connection settings for each.
///
/// <para>Wi-Fi endpoints default to the near-universal <c>192.168.0.10:35000</c>;
/// serial/classic-Bluetooth baud defaults to 38400. BLE-only dongles are listed
/// for transparency but are not yet usable (no BLE transport).</para>
/// </summary>
public static class KnownAdapters
{
    private const string WiFiEndpoint = "192.168.0.10:35000";

    private static readonly KnownAdapter[] Catalog =
    [
        // --- USB / Wi-Fi / classic Bluetooth: supported today ---
        new("bafx-bt", "BAFX Products (Bluetooth)", DongleLink.BluetoothClassic, AdapterChip.Elm327,
            null, 38400, "standard", "Popular classic-Bluetooth ELM327 for Android/Windows."),
        new("veepeak-wifi", "Veepeak OBDCheck Mini WiFi", DongleLink.WiFi, AdapterChip.Elm327,
            WiFiEndpoint, 38400, "standard", "Wi-Fi ELM327; join its AP, then connect."),
        new("veepeak-bt", "Veepeak Mini Bluetooth", DongleLink.BluetoothClassic, AdapterChip.Elm327,
            null, 38400, "standard", "Classic-Bluetooth ELM327."),
        new("vgate-icar-pro-bt", "Vgate iCar Pro (Bluetooth 3.0)", DongleLink.BluetoothClassic, AdapterChip.Elm327,
            null, 38400, "launch", "Classic-Bluetooth clone; tolerant timing helps."),
        new("vgate-icar-wifi", "Vgate iCar Pro WiFi", DongleLink.WiFi, AdapterChip.Elm327,
            WiFiEndpoint, 38400, "launch", "Wi-Fi clone; tolerant timing helps."),
        new("vlinker-fs", "vLinker FS (USB)", DongleLink.Usb, AdapterChip.Elm327,
            null, 38400, "standard", "USB adapter, good for PC logging."),
        new("vlinker-mc", "vLinker MC+ (Bluetooth)", DongleLink.BluetoothClassic, AdapterChip.Elm327,
            null, 38400, "standard", "Dual-mode; classic-Bluetooth path is supported."),
        new("panlong-bt", "Panlong Bluetooth", DongleLink.BluetoothClassic, AdapterChip.Elm327,
            null, 38400, "launch", "Cheap classic-Bluetooth clone."),
        new("panlong-wifi", "Panlong WiFi", DongleLink.WiFi, AdapterChip.Elm327,
            WiFiEndpoint, 38400, "launch", "Cheap Wi-Fi clone."),
        new("kobra-wifi", "KOBRA WiFi", DongleLink.WiFi, AdapterChip.Elm327,
            WiFiEndpoint, 38400, "launch", "Cheap Wi-Fi clone."),
        new("kobra-bt", "KOBRA Bluetooth", DongleLink.BluetoothClassic, AdapterChip.Elm327,
            null, 38400, "launch", "Cheap classic-Bluetooth clone."),
        new("obdlink-lx", "OBDLink LX (Bluetooth)", DongleLink.BluetoothClassic, AdapterChip.Stn,
            null, 115200, "standard", "Quality STN chip over classic Bluetooth."),
        new("obdlink-mxplus", "OBDLink MX+ (Bluetooth)", DongleLink.BluetoothClassic, AdapterChip.Stn,
            null, 115200, "standard", "STN chip; classic-Bluetooth path is supported on desktop."),
        new("generic-usb", "Generic ELM327 (USB)", DongleLink.Usb, AdapterChip.Elm327,
            null, 38400, "launch", "No-name USB clone; tolerant timing recommended."),
        new("generic-wifi", "Generic ELM327 (WiFi)", DongleLink.WiFi, AdapterChip.Elm327,
            WiFiEndpoint, 38400, "launch", "No-name Wi-Fi clone."),
        new("generic-bt", "Generic ELM327 (Bluetooth)", DongleLink.BluetoothClassic, AdapterChip.Elm327,
            null, 38400, "launch", "No-name classic-Bluetooth clone."),

        // --- BLE-only: listed for transparency, not yet usable ---
        new("veepeak-ble", "Veepeak OBDCheck BLE / BLE+", DongleLink.BluetoothLe, AdapterChip.Elm327,
            null, 0, "standard", "Bluetooth LE only — needs a BLE transport (not yet supported)."),
        new("obdlink-cx", "OBDLink CX", DongleLink.BluetoothLe, AdapterChip.Stn,
            null, 0, "standard", "Bluetooth LE only — needs a BLE transport (not yet supported)."),
        new("vgate-icar-pro-ble", "Vgate iCar Pro BLE 4.0", DongleLink.BluetoothLe, AdapterChip.Elm327,
            null, 0, "standard", "Bluetooth LE only — needs a BLE transport (not yet supported)."),
    ];

    private static readonly ReadOnlyDictionary<string, KnownAdapter> ByKeyMap =
        new(Catalog.ToDictionary(a => a.Key, StringComparer.OrdinalIgnoreCase));

    /// <summary>Gets every known dongle (supported and not).</summary>
    public static IReadOnlyList<KnownAdapter> All => Catalog;

    /// <summary>Gets the dongles this tool can currently talk to.</summary>
    public static IReadOnlyList<KnownAdapter> Supported => [.. Catalog.Where(a => a.IsSupported)];

    /// <summary>Looks up a dongle by key (case-insensitive).</summary>
    /// <param name="key">The dongle key, e.g. <c>veepeak-wifi</c>.</param>
    /// <param name="adapter">The matching dongle, if found.</param>
    /// <returns><see langword="true"/> if a dongle was found.</returns>
    public static bool TryGet(string key, out KnownAdapter? adapter)
    {
        ArgumentNullException.ThrowIfNull(key);
        return ByKeyMap.TryGetValue(key, out adapter);
    }
}
