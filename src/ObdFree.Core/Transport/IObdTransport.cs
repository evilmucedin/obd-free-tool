namespace ObdFree.Core.Transport;

/// <summary>
/// Abstraction over a byte stream to an OBD-II adapter (ELM327 family).
/// Concrete implementations talk to USB serial, Bluetooth RFCOMM, or a
/// TCP/Wi-Fi adapter. Tests use an in-memory fake so no hardware is required.
/// </summary>
public interface IObdTransport : IAsyncDisposable
{
    /// <summary>Gets a value indicating whether the transport is currently open.</summary>
    bool IsOpen { get; }

    /// <summary>Opens the underlying connection.</summary>
    Task OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Closes the underlying connection.</summary>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command (without the trailing carriage return) and returns the
    /// adapter's response with the terminating prompt character stripped.
    /// </summary>
    Task<string> SendCommandAsync(string command, CancellationToken cancellationToken = default);
}
