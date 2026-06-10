using ObdFree.Core.Protocol;

namespace ObdFree.Core.Transport;

/// <summary>
/// Base class for transports that speak the ELM327 line protocol over a
/// <see cref="Stream"/> (serial port, TCP socket, Bluetooth SPP, …). Subclasses
/// only need to open and expose the underlying stream.
/// </summary>
public abstract class StreamObdTransport : IObdTransport
{
    private Stream? _stream;
    private IDisposable? _owner;

    /// <summary>Gets the per-command read timeout.</summary>
    protected TimeSpan ReadTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public bool IsOpen => _stream is not null;

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_stream is not null)
        {
            return;
        }

        (_stream, _owner) = await OpenStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        Stream? stream = _stream;
        IDisposable? owner = _owner;
        _stream = null;
        _owner = null;
        stream?.Dispose();

        // Dispose the native handle (SerialPort / TcpClient) if it is distinct.
        if (!ReferenceEquals(owner, stream))
        {
            owner?.Dispose();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<string> SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_stream is null)
        {
            throw new InvalidOperationException("Transport is not open. Call OpenAsync first.");
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ReadTimeout);

        await ElmIo.WriteCommandAsync(_stream, command, cts.Token).ConfigureAwait(false);
        return await ElmIo.ReadUntilPromptAsync(_stream, cts.Token).ConfigureAwait(false);
    }

    /// <summary>Opens the underlying stream. Implemented by concrete transports.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// The opened stream and the native handle that owns it (e.g. the
    /// <see cref="System.IO.Ports.SerialPort"/> or socket) so it can be disposed.
    /// </returns>
    protected abstract Task<(Stream Stream, IDisposable Owner)> OpenStreamAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CloseAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
