using ObdFree.Core.Modes;
using ObdFree.Core.Transport;

namespace ObdFree.Core.Config;

/// <summary>
/// User-configurable, persisted application settings. Saved to disk so the app
/// resumes with the same choices after a restart.
/// </summary>
public sealed class AppConfig
{
    /// <summary>Gets or sets the operating mode (defaults to Safe).</summary>
    public OperatingMode Mode { get; set; } = OperatingMode.Safe;

    /// <summary>Gets or sets the last-used connection kind.</summary>
    public ConnectionKind ConnectionKind { get; set; } = ConnectionKind.Usb;

    /// <summary>Gets or sets the last-used target (serial port or <c>host:port</c>).</summary>
    public string Target { get; set; } = "/dev/ttyUSB0";

    /// <summary>Gets or sets the last-used serial baud rate.</summary>
    public int BaudRate { get; set; } = 38400;

    /// <summary>Gets or sets the last-used vehicle profile key (e.g. <c>toyota</c>).</summary>
    public string VehicleProfileKey { get; set; } = "generic";

    /// <summary>Gets or sets the last-used adapter profile key (e.g. <c>standard</c>).</summary>
    public string AdapterProfileKey { get; set; } = "standard";
}
