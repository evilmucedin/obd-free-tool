using System.IO.Ports;

namespace ObdFree.Core.Transport;

/// <summary>
/// Talks to an ELM327 adapter over a serial port. Covers both USB adapters
/// (e.g. <c>/dev/ttyUSB0</c>, <c>COM3</c>) and classic Bluetooth (SPP) adapters,
/// which the OS exposes as a serial device (e.g. <c>/dev/rfcomm0</c>, a COM port).
/// </summary>
public sealed class SerialObdTransport : StreamObdTransport
{
    private readonly string _portName;
    private readonly int _baudRate;

    /// <summary>Initializes a new serial transport.</summary>
    /// <param name="portName">The serial port name, e.g. <c>/dev/ttyUSB0</c> or <c>COM3</c>.</param>
    /// <param name="baudRate">The baud rate (ELM327 clones commonly use 38400 or 115200).</param>
    public SerialObdTransport(string portName, int baudRate = 38400)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        _portName = portName;
        _baudRate = baudRate;
    }

    /// <inheritdoc />
    protected override Task<(Stream Stream, IDisposable Owner)> OpenStreamAsync(CancellationToken cancellationToken)
    {
        var port = new SerialPort(_portName, _baudRate)
        {
            ReadTimeout = (int)ReadTimeout.TotalMilliseconds,
            WriteTimeout = (int)ReadTimeout.TotalMilliseconds,
            NewLine = "\r",
            DtrEnable = true,
            RtsEnable = true,
        };

        port.Open();
        return Task.FromResult<(Stream, IDisposable)>((port.BaseStream, port));
    }
}
