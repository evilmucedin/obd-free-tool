using ObdFree.Core.Transport;

namespace ObdFree.Core.Adapters;

/// <summary>The physical link a dongle uses to talk to the host.</summary>
public enum DongleLink
{
    /// <summary>USB / serial.</summary>
    Usb,

    /// <summary>Wi-Fi access point (TCP).</summary>
    WiFi,

    /// <summary>Classic Bluetooth (SPP), exposed by the OS as a serial port.</summary>
    BluetoothClassic,

    /// <summary>Bluetooth Low Energy (GATT) — not yet supported by this tool.</summary>
    BluetoothLe,
}

/// <summary>The interpreter chip family.</summary>
public enum AdapterChip
{
    /// <summary>ELM327 or a clone of it.</summary>
    Elm327,

    /// <summary>ScanTool STN (OBDLink) — a superset of ELM327.</summary>
    Stn,
}

/// <summary>
/// A specific, popular off-the-shelf OBD-II dongle (the kind sold on Amazon),
/// with the settings needed to connect to it. Used to auto-configure the
/// connection so users don't have to know baud rates or Wi-Fi endpoints.
/// </summary>
/// <param name="Key">Stable lowercase key used on the CLI, e.g. <c>veepeak-wifi</c>.</param>
/// <param name="DisplayName">Human-readable product name.</param>
/// <param name="Link">The physical link the dongle uses.</param>
/// <param name="Chip">The interpreter chip family.</param>
/// <param name="DefaultEndpoint">Wi-Fi <c>host:port</c> (Wi-Fi dongles only).</param>
/// <param name="DefaultBaud">Serial baud rate (USB / classic Bluetooth).</param>
/// <param name="AdapterProfileKey">Recommended <see cref="AdapterProfile"/> key.</param>
/// <param name="Notes">Short guidance shown to the user.</param>
public sealed record KnownAdapter(
    string Key,
    string DisplayName,
    DongleLink Link,
    AdapterChip Chip,
    string? DefaultEndpoint,
    int DefaultBaud,
    string AdapterProfileKey,
    string Notes)
{
    /// <summary>
    /// Gets whether this tool can currently talk to the dongle. BLE adapters are
    /// not yet supported (no BLE/GATT transport).
    /// </summary>
    public bool IsSupported => Link != DongleLink.BluetoothLe;

    /// <summary>Gets whether connecting needs a serial port from the user.</summary>
    public bool RequiresSerialPort => Link is DongleLink.Usb or DongleLink.BluetoothClassic;

    /// <summary>Gets the recommended adapter profile.</summary>
    public AdapterProfile AdapterProfile =>
        AdapterProfiles.TryGet(AdapterProfileKey, out AdapterProfile? p) && p is not null
            ? p
            : AdapterProfiles.Standard;

    /// <summary>
    /// Builds the connection for this dongle. Wi-Fi dongles use their known
    /// endpoint; USB / classic-Bluetooth dongles need a serial port supplied.
    /// </summary>
    /// <param name="serialPort">The serial port for USB / classic-Bluetooth dongles.</param>
    /// <returns>The connection.</returns>
    /// <exception cref="InvalidOperationException">BLE dongle, or a required port is missing.</exception>
    public ObdConnection CreateConnection(string? serialPort = null)
    {
        switch (Link)
        {
            case DongleLink.WiFi:
                return ObdConnection.WiFi(DefaultEndpoint ?? $"{TcpObdTransport.DefaultHost}:{TcpObdTransport.DefaultPort}");
            case DongleLink.Usb when !string.IsNullOrWhiteSpace(serialPort):
                return ObdConnection.Usb(serialPort, DefaultBaud);
            case DongleLink.BluetoothClassic when !string.IsNullOrWhiteSpace(serialPort):
                return ObdConnection.Bluetooth(serialPort, DefaultBaud);
            case DongleLink.Usb or DongleLink.BluetoothClassic:
                throw new InvalidOperationException(
                    $"{DisplayName} needs a serial port. Pass --usb <port> or --bluetooth <port>.");
            default:
                throw new InvalidOperationException(
                    $"{DisplayName} uses Bluetooth LE, which is not supported yet.");
        }
    }
}
