using System.Net.Sockets;

namespace ObdFree.Core.Transport;

/// <summary>
/// Talks to a Wi-Fi ELM327 adapter over TCP. These adapters typically listen on
/// <c>192.168.0.10:35000</c> when you join their access point.
/// </summary>
public sealed class TcpObdTransport : StreamObdTransport
{
    /// <summary>The default host for most Wi-Fi ELM327 adapters.</summary>
    public const string DefaultHost = "192.168.0.10";

    /// <summary>The default TCP port for most Wi-Fi ELM327 adapters.</summary>
    public const int DefaultPort = 35000;

    private readonly string _host;
    private readonly int _port;

    /// <summary>Initializes a new Wi-Fi transport.</summary>
    /// <param name="host">The adapter host or IP address.</param>
    /// <param name="port">The adapter TCP port.</param>
    public TcpObdTransport(string host = DefaultHost, int port = DefaultPort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        _host = host;
        _port = port;
    }

    /// <summary>
    /// Parses an <c>host:port</c> endpoint string. A bare host uses
    /// <see cref="DefaultPort"/>.
    /// </summary>
    /// <param name="endpoint">The endpoint, e.g. <c>192.168.0.10:35000</c>.</param>
    /// <returns>A configured transport.</returns>
    public static TcpObdTransport FromEndpoint(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        int colon = endpoint.LastIndexOf(':');
        if (colon < 0)
        {
            return new TcpObdTransport(endpoint);
        }

        string host = endpoint[..colon];
        string portText = endpoint[(colon + 1)..];
        if (!int.TryParse(portText, out int port))
        {
            throw new FormatException($"Invalid endpoint port: '{endpoint}'.");
        }

        return new TcpObdTransport(host, port);
    }

    /// <inheritdoc />
    protected override async Task<(Stream Stream, IDisposable Owner)> OpenStreamAsync(CancellationToken cancellationToken)
    {
        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(_host, _port, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            client.Dispose();
            throw;
        }

        return (client.GetStream(), client);
    }
}
