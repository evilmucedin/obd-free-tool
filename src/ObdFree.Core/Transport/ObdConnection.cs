namespace ObdFree.Core.Transport;

/// <summary>How the tool connects to the OBD-II adapter.</summary>
public enum ConnectionKind
{
    /// <summary>USB / serial adapter.</summary>
    Usb,

    /// <summary>Wi-Fi adapter reached over TCP.</summary>
    WiFi,

    /// <summary>Classic Bluetooth (SPP) adapter, exposed by the OS as a serial port.</summary>
    Bluetooth,
}

/// <summary>
/// A description of how to reach an adapter, and a factory to build the matching
/// <see cref="IObdTransport"/>. This is what the CLI populates from its flags.
/// </summary>
public sealed class ObdConnection
{
    private ObdConnection(ConnectionKind kind, string target, int baudRate)
    {
        Kind = kind;
        Target = target;
        BaudRate = baudRate;
    }

    /// <summary>Gets the connection kind.</summary>
    public ConnectionKind Kind { get; }

    /// <summary>Gets the target: a serial port name, or a <c>host:port</c> endpoint.</summary>
    public string Target { get; }

    /// <summary>Gets the serial baud rate (USB/Bluetooth only).</summary>
    public int BaudRate { get; }

    /// <summary>Creates a USB/serial connection.</summary>
    /// <param name="portName">Serial port, e.g. <c>/dev/ttyUSB0</c> or <c>COM3</c>.</param>
    /// <param name="baudRate">Serial baud rate.</param>
    /// <returns>The connection descriptor.</returns>
    public static ObdConnection Usb(string portName, int baudRate = 38400)
        => new(ConnectionKind.Usb, portName, baudRate);

    /// <summary>Creates a Wi-Fi connection.</summary>
    /// <param name="endpoint">A <c>host:port</c> endpoint, or bare host.</param>
    /// <returns>The connection descriptor.</returns>
    public static ObdConnection WiFi(string endpoint)
        => new(ConnectionKind.WiFi, endpoint, 0);

    /// <summary>Creates a Bluetooth (SPP) connection.</summary>
    /// <param name="portName">The serial device the OS bound to the adapter.</param>
    /// <param name="baudRate">Serial baud rate.</param>
    /// <returns>The connection descriptor.</returns>
    public static ObdConnection Bluetooth(string portName, int baudRate = 38400)
        => new(ConnectionKind.Bluetooth, portName, baudRate);

    /// <summary>Builds the transport for this connection.</summary>
    /// <returns>A new, unopened <see cref="IObdTransport"/>.</returns>
    public IObdTransport CreateTransport() => Kind switch
    {
        ConnectionKind.WiFi => TcpObdTransport.FromEndpoint(Target),
        ConnectionKind.Usb or ConnectionKind.Bluetooth => new SerialObdTransport(Target, BaudRate),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unsupported connection kind."),
    };

    /// <inheritdoc />
    public override string ToString() => Kind == ConnectionKind.WiFi
        ? $"{Kind} ({Target})"
        : $"{Kind} ({Target} @ {BaudRate} baud)";
}
